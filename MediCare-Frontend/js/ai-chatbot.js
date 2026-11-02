/**
 * MEDICARE PLATFORM - GENERAL PURPOSE AI CHATBOT COMPONENT
 * File: js/ai-chatbot.js
 * Description: Universal ChatGPT-like AI Assistant for all authenticated
 *              MediCare pages. Strictly excluded from index.html and login.html.
 */

(function (window, document) {
  'use strict';

  // 1. STRICT EXCLUSIONS GUARD
  function getNormalizedPageName() {
    try {
      const path = (window.location.pathname || '').replace(/\/+$/, '');
      const file = path.split('/').pop() || 'index.html';
      return file.toLowerCase();
    } catch (e) {
      return '';
    }
  }

  const currentPage = getNormalizedPageName();
  const EXCLUDED_PAGES = ['index.html', 'login.html', ''];
  if (EXCLUDED_PAGES.includes(currentPage)) {
    // Strictly do not load or render on public / login pages
    return;
  }

  // Prevent duplicate initialization
  if (window.__MEDICARE_CHATBOT_INITIALIZED__) {
    return;
  }
  window.__MEDICARE_CHATBOT_INITIALIZED__ = true;

  // 2. CONSTANTS & STORAGE KEYS
  const STORAGE_KEY_PANEL_OPEN = 'medicare_chat_is_open';

  // State
  let isSending = false;
  let chatHistory = [];

  // 3. AUTHENTICATION & API RESOLUTION
  function getAuthToken() {
    if (window.MediCareAPI && typeof window.MediCareAPI.getAuthToken === 'function') {
      const tok = window.MediCareAPI.getAuthToken();
      if (tok) return tok;
    }
    return (
      localStorage.getItem('medicare_jwt_token') ||
      localStorage.getItem('jwtToken') ||
      localStorage.getItem('token') ||
      null
    );
  }

  function getCurrentUser() {
    if (window.MediCareAPI && typeof window.MediCareAPI.getCurrentUser === 'function') {
      const u = window.MediCareAPI.getCurrentUser();
      if (u) return u;
    }
    try {
      const raw = localStorage.getItem('medicare_user');
      return raw ? JSON.parse(raw) : null;
    } catch (e) {
      return null;
    }
  }

  function getApiBaseUrl() {
    if (window.MediCareAPI && window.MediCareAPI.API_BASE_URL) {
      return window.MediCareAPI.API_BASE_URL;
    }
    if (typeof window !== 'undefined' && window.location) {
      const port = String(window.location.port || '');
      if (port === '5000' || port === '5095') {
        return `${window.location.origin}/api`;
      }
    }
    return 'http://localhost:5000/api';
  }

  // 4. HTML MARKDOWN / TEXT FORMATTING HELPER
  function escapeHtml(text) {
    if (!text) return '';
    return String(text)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function formatAiReply(rawText) {
    if (!rawText) return '';

    // Format fenced code blocks ```lang ... ```
    let text = rawText.replace(/```([a-zA-Z0-9_-]*)\n([\s\S]*?)```/g, function (match, lang, code) {
      return `<pre style="background:#0f172a; color:#f8fafc; padding:9px 12px; border-radius:8px; overflow-x:auto; font-size:11px; margin:6px 0; font-family:monospace; line-height:1.4;"><code>${escapeHtml(code.trim())}</code></pre>`;
    });

    // Format inline code `code`
    text = text.replace(/`([^`]+)`/g, '<code style="background:#e2e8f0; color:#0f172a; padding:1px 4px; border-radius:4px; font-size:11px; font-family:monospace;">$1</code>');

    // Split into lines for list and paragraph processing
    const lines = text.split('\n');
    let formatted = [];
    let inList = false;

    for (let line of lines) {
      const trimmed = line.trim();

      if (trimmed.startsWith('<pre') || trimmed.endsWith('</pre>') || trimmed.startsWith('<code>') || trimmed.endsWith('</code>')) {
        if (inList) {
          formatted.push('</ul>');
          inList = false;
        }
        formatted.push(line);
        continue;
      }

      // Bullet points
      if (trimmed.startsWith('* ') || trimmed.startsWith('- ')) {
        if (!inList) {
          formatted.push('<ul>');
          inList = true;
        }
        let content = trimmed.substring(2);
        content = content.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        formatted.push(`<li>${content}</li>`);
      } else {
        if (inList) {
          formatted.push('</ul>');
          inList = false;
        }

        if (trimmed.length > 0) {
          let content = trimmed.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
          formatted.push(`<p>${content}</p>`);
        }
      }
    }

    if (inList) {
      formatted.push('</ul>');
    }

    return formatted.join('');
  }

  // 5. INJECT WIDGET DOM
  function createWidgetElements() {
    // Launcher Floating Button
    const launcher = document.createElement('button');
    launcher.id = 'medicareChatLauncher';
    launcher.className = 'medicare-chat-launcher';
    launcher.setAttribute('aria-label', 'Open AI Assistant');
    launcher.setAttribute('title', 'AI Assistant (ChatGPT)');
    launcher.setAttribute('type', 'button');
    launcher.innerHTML = `
      <div class="medicare-launcher-icon-wrap">
        <span>🤖</span>
        <span class="medicare-launcher-status-dot"></span>
      </div>
      <div class="medicare-launcher-label">
        <span class="medicare-launcher-title">AI Assistant</span>
      </div>
    `;

    // Chat Panel
    const panel = document.createElement('div');
    panel.id = 'medicareChatPanel';
    panel.className = 'medicare-chat-panel';
    panel.innerHTML = `
      <!-- Header -->
      <div class="medicare-chat-header">
        <div class="medicare-chat-header-info">
          <div class="medicare-chat-avatar">🤖</div>
          <div class="medicare-chat-titles">
            <h3>AI Assistant</h3>
            <span class="medicare-chat-status">
              <span class="medicare-chat-status-dot"></span> Online &bull; Ready to help
            </span>
          </div>
        </div>
        <div class="medicare-chat-header-actions">
          <button type="button" class="medicare-chat-head-btn" id="medicareChatClearBtn" title="New Chat (Clear Conversation)">
            🔄
          </button>
          <button type="button" class="medicare-chat-head-btn" id="medicareChatCloseBtn" title="Close">
            ✕
          </button>
        </div>
      </div>

      <!-- Messages Stream -->
      <div class="medicare-chat-messages" id="medicareChatMessages"></div>

      <!-- Quick Suggestion Chips -->
      <div class="medicare-chat-prompts" id="medicareChatPrompts">
        <button type="button" class="medicare-chat-chip" data-prompt="Explain dependency injection in ASP.NET Core">💻 Dependency Injection</button>
        <button type="button" class="medicare-chat-chip" data-prompt="Write a Java program to reverse a string">☕ Java String Reverse</button>
        <button type="button" class="medicare-chat-chip" data-prompt="Help me improve my English communication">🗣️ English Practice</button>
        <button type="button" class="medicare-chat-chip" data-prompt="What is MediCare, and how does its medicine schedule work?">🏥 What is MediCare?</button>
      </div>

      <!-- Input Bar -->
      <form class="medicare-chat-input-bar" id="medicareChatForm">
        <input 
          type="text" 
          id="medicareChatInput" 
          class="medicare-chat-input" 
          placeholder="Ask anything (e.g. coding, questions, ideas)..." 
          autocomplete="off"
        />
        <button type="submit" id="medicareChatSendBtn" class="medicare-chat-send-btn">
          <span>Send</span> ➤
        </button>
      </form>
    `;

    document.body.appendChild(launcher);
    document.body.appendChild(panel);
  }

  // 6. CHAT RENDER & PERSISTENCE
  function getHistoryStorageKey() {
    const user = getCurrentUser();
    const uid = user && (user.id || user.email) ? String(user.id || user.email).toLowerCase() : 'user';
    return `medicare_chat_history_${uid}`;
  }

  function loadStoredHistory() {
    try {
      const raw = sessionStorage.getItem(getHistoryStorageKey());
      if (raw) {
        const parsed = JSON.parse(raw);
        if (Array.isArray(parsed) && parsed.length > 0) {
          return parsed;
        }
      }
    } catch (e) {
      console.warn('[MediCare Chatbot] Could not load saved history:', e);
    }
    return null;
  }

  function saveHistory() {
    try {
      sessionStorage.setItem(getHistoryStorageKey(), JSON.stringify(chatHistory));
    } catch (e) {
      console.warn('[MediCare Chatbot] Could not save history:', e);
    }
  }

  function renderMessages() {
    const container = document.getElementById('medicareChatMessages');
    if (!container) return;
    container.innerHTML = '';

    chatHistory.forEach((msg) => {
      appendMessageToDOM(msg.text, msg.isUser, msg.time, msg.isFormatted, false);
    });

    container.scrollTop = container.scrollHeight;
  }

  function appendMessageToDOM(text, isUser, timeStr = null, isFormatted = false, scroll = true) {
    const container = document.getElementById('medicareChatMessages');
    if (!container) return;

    if (!timeStr) {
      const now = new Date();
      timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    const row = document.createElement('div');
    row.className = `medicare-chat-row ${isUser ? 'user' : 'bot'}`;

    const bubbleContent = isUser ? escapeHtml(text) : (isFormatted ? text : formatAiReply(text));

    row.innerHTML = `
      <div>
        <div class="medicare-chat-bubble">${bubbleContent}</div>
        <div class="medicare-chat-timestamp">${timeStr}</div>
      </div>
    `;

    container.appendChild(row);
    if (scroll) {
      container.scrollTop = container.scrollHeight;
    }
  }

  function addMessage(text, isUser, isFormatted = false) {
    const now = new Date();
    const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    chatHistory.push({
      text,
      isUser,
      time: timeStr,
      isFormatted
    });

    // Limit history length to last 40 messages
    if (chatHistory.length > 40) {
      chatHistory.shift();
    }

    saveHistory();
    appendMessageToDOM(text, isUser, timeStr, isFormatted, true);
  }

  function showTypingIndicator() {
    const container = document.getElementById('medicareChatMessages');
    if (!container) return;

    const typingEl = document.createElement('div');
    typingEl.id = 'medicareChatTypingIndicator';
    typingEl.className = 'medicare-chat-row bot';
    typingEl.innerHTML = `
      <div>
        <div class="medicare-chat-typing">
          <span class="medicare-chat-typing-dot"></span>
          <span class="medicare-chat-typing-dot"></span>
          <span class="medicare-chat-typing-dot"></span>
        </div>
        <div class="medicare-chat-timestamp" style="font-style: italic; color: #64748b;">AI is thinking...</div>
      </div>
    `;
    container.appendChild(typingEl);
    container.scrollTop = container.scrollHeight;
  }

  function hideTypingIndicator() {
    const el = document.getElementById('medicareChatTypingIndicator');
    if (el) el.remove();
  }

  function updateQuickSuggestions(suggestions) {
    const container = document.getElementById('medicareChatPrompts');
    if (!container) return;

    if (Array.isArray(suggestions) && suggestions.length > 0) {
      container.innerHTML = suggestions
        .map((s) => {
          const safe = escapeHtml(s);
          return `<button type="button" class="medicare-chat-chip" data-prompt="${safe}">💡 ${safe}</button>`;
        })
        .join('');
      container.style.display = 'flex';
    } else {
      // Keep existing starter prompts visible
      container.style.display = 'flex';
    }
  }

  // 7. API COMMUNICATION (Multi-turn conversation support)
  async function sendQueryToAi(userQuery) {
    if (isSending) return;
    const trimmed = (userQuery || '').trim();
    if (!trimmed) return;

    const token = getAuthToken();
    if (!token) {
      addMessage('⚠️ Your session appears unauthenticated. Please refresh or log in again.', false);
      return;
    }

    // Add User Message
    addMessage(trimmed, true);

    // Disable input state
    isSending = true;
    const inputEl = document.getElementById('medicareChatInput');
    const sendBtn = document.getElementById('medicareChatSendBtn');
    if (inputEl) {
      inputEl.value = '';
      inputEl.disabled = true;
    }
    if (sendBtn) sendBtn.disabled = true;

    showTypingIndicator();

    // Prepare multi-turn history for contextual conversation
    const historyPayload = chatHistory
      .filter((m) => m.text && !m.text.startsWith('⚠️') && !m.text.startsWith('Hello! I'))
      .slice(-8)
      .map((m) => ({
        role: m.isUser ? 'user' : 'model',
        content: m.text
      }));

    try {
      let replyText = '';
      let quickSuggestions = [];

      if (window.MediCareAPI && typeof window.MediCareAPI.apiFetch === 'function') {
        const res = await window.MediCareAPI.apiFetch('/chat/ask', 'POST', {
          message: trimmed,
          history: historyPayload
        });
        replyText = res.reply || 'No response received from AI service.';
        quickSuggestions = res.quickSuggestions || [];
      } else {
        const apiUrl = `${getApiBaseUrl()}/chat/ask`;
        const res = await fetch(apiUrl, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify({
            message: trimmed,
            history: historyPayload
          })
        });

        if (!res.ok) {
          if (res.status === 401) {
            throw new Error('Your session has expired. Please log in again.');
          }
          const errData = await res.json().catch(() => ({}));
          throw new Error(errData.message || `Server error (${res.status})`);
        }

        const data = await res.json();
        replyText = data.reply || 'No response received from AI service.';
        quickSuggestions = data.quickSuggestions || [];
      }

      hideTypingIndicator();
      addMessage(replyText, false, false);
      updateQuickSuggestions(quickSuggestions);
    } catch (err) {
      console.error('[MediCare Chatbot] Error:', err);
      hideTypingIndicator();
      addMessage(`⚠️ ${err.message || 'Unable to connect to AI service. Please verify your connection.'}`, false);
    } finally {
      isSending = false;
      if (inputEl) {
        inputEl.disabled = false;
        inputEl.focus();
      }
      if (sendBtn) sendBtn.disabled = false;
    }
  }

  // 8. PANEL VISIBILITY CONTROLS
  function openChatPanel() {
    const panel = document.getElementById('medicareChatPanel');
    const launcher = document.getElementById('medicareChatLauncher');
    if (!panel) return;

    panel.classList.add('active');
    if (launcher) launcher.style.display = 'none';

    try {
      sessionStorage.setItem(STORAGE_KEY_PANEL_OPEN, 'true');
    } catch (e) {}

    const input = document.getElementById('medicareChatInput');
    if (input) setTimeout(() => input.focus(), 150);

    const container = document.getElementById('medicareChatMessages');
    if (container) container.scrollTop = container.scrollHeight;
  }

  function closeChatPanel() {
    const panel = document.getElementById('medicareChatPanel');
    const launcher = document.getElementById('medicareChatLauncher');
    if (!panel) return;

    panel.classList.remove('active');
    if (launcher) launcher.style.display = 'flex';

    try {
      sessionStorage.removeItem(STORAGE_KEY_PANEL_OPEN);
    } catch (e) {}
  }

  function toggleChatPanel() {
    const panel = document.getElementById('medicareChatPanel');
    if (!panel) return;
    if (panel.classList.contains('active')) {
      closeChatPanel();
    } else {
      openChatPanel();
    }
  }

  function clearConversation() {
    if (confirm('Start a new conversation? Current chat history will be cleared.')) {
      chatHistory = [];
      sessionStorage.removeItem(getHistoryStorageKey());

      const welcomeMsg = "Hello! I'm your AI assistant. How can I help you today? Feel free to ask about programming, general knowledge, writing, MediCare, or any topic you'd like!";
      addMessage(welcomeMsg, false, false);

      const promptsBar = document.getElementById('medicareChatPrompts');
      if (promptsBar) {
        promptsBar.innerHTML = `
          <button type="button" class="medicare-chat-chip" data-prompt="Explain dependency injection in ASP.NET Core">💻 Dependency Injection</button>
          <button type="button" class="medicare-chat-chip" data-prompt="Write a Java program to reverse a string">☕ Java String Reverse</button>
          <button type="button" class="medicare-chat-chip" data-prompt="Help me improve my English communication">🗣️ English Practice</button>
          <button type="button" class="medicare-chat-chip" data-prompt="What is MediCare, and how does its medicine schedule work?">🏥 What is MediCare?</button>
        `;
      }
    }
  }

  // 9. EVENT BINDINGS
  function initEventHandlers() {
    const launcher = document.getElementById('medicareChatLauncher');
    const closeBtn = document.getElementById('medicareChatCloseBtn');
    const clearBtn = document.getElementById('medicareChatClearBtn');
    const form = document.getElementById('medicareChatForm');
    const input = document.getElementById('medicareChatInput');
    const promptsBar = document.getElementById('medicareChatPrompts');

    if (launcher) {
      launcher.addEventListener('click', openChatPanel);
    }

    if (closeBtn) {
      closeBtn.addEventListener('click', closeChatPanel);
    }

    if (clearBtn) {
      clearBtn.addEventListener('click', clearConversation);
    }

    if (form) {
      form.addEventListener('submit', function (e) {
        e.preventDefault();
        if (input) {
          sendQueryToAi(input.value);
        }
      });
    }

    if (promptsBar) {
      promptsBar.addEventListener('click', function (e) {
        const chip = e.target.closest('.medicare-chat-chip');
        if (chip) {
          const prompt = chip.getAttribute('data-prompt');
          if (prompt) {
            sendQueryToAi(prompt);
          }
        }
      });
    }

    // Keyboard support: Escape key closes the chat panel
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') {
        const panel = document.getElementById('medicareChatPanel');
        if (panel && panel.classList.contains('active')) {
          closeChatPanel();
        }
      }
    });
  }

  // 10. BOOTSTRAP INITIALIZATION
  function initChatbot() {
    if (document.getElementById('medicareChatPanel')) return;

    createWidgetElements();
    initEventHandlers();

    // Initialize or restore conversation
    const saved = loadStoredHistory();
    if (saved && saved.length > 0) {
      chatHistory = saved;
      renderMessages();
    } else {
      const welcomeMsg = "Hello! I'm your AI assistant. How can I help you today? Feel free to ask about programming, general knowledge, writing, MediCare, or any topic you'd like!";
      addMessage(welcomeMsg, false, false);
    }

    // Check if panel was open before navigation
    try {
      if (sessionStorage.getItem(STORAGE_KEY_PANEL_OPEN) === 'true') {
        openChatPanel();
      }
    } catch (e) {}
  }

  // Safe document ready handler
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initChatbot);
  } else {
    initChatbot();
  }

  // Expose to window for testing
  window.MediCareChatbot = {
    open: openChatPanel,
    close: closeChatPanel,
    toggle: toggleChatPanel,
    send: sendQueryToAi,
    clear: clearConversation
  };

})(window, document);
