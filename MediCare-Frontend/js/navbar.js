/**
 * MEDICARE PLATFORM - MODULAR HEADER & MOBILE NAVIGATION DRAWER
 * File: js/navbar.js
 * Description: Client-side dynamic injection for the unified navigation header,
 *              responsive action cluster, dynamic role indicators, slide-in drawer,
 *              strict route guards, and unauthenticated access prevention.
 */

(function (window, document) {
  'use strict';

  // 1. PUBLIC PAGES (Always accessible without login or registration)
  const PUBLIC_PAGES = ['index.html', 'login.html', ''];

  // 2. PROTECTED ROUTES & ROLE PERMISSIONS
  const ROUTE_PERMISSIONS = {
    'patient-dashboard.html': ['PATIENT', 'ADMIN'],
    'caregiver-dashboard.html': ['CAREGIVER', 'ADMIN'],
    'admin-dashboard.html': ['ADMIN'],
    'medicines.html': ['PATIENT', 'CAREGIVER', 'ADMIN'],
    'history.html': ['PATIENT', 'CAREGIVER', 'ADMIN'],
    'notifications.html': ['PATIENT', 'CAREGIVER', 'ADMIN'],
    'settings.html': ['PATIENT', 'CAREGIVER', 'ADMIN'],
    'profile-settings.html': ['PATIENT', 'CAREGIVER', 'ADMIN'],
    'emergency.html': ['PATIENT', 'CAREGIVER', 'ADMIN'],
    'ai-assistant.html': ['PATIENT', 'CAREGIVER', 'ADMIN'],
    'print-report.html': ['PATIENT', 'CAREGIVER', 'ADMIN'],
  };

  // 3. PAGE METADATA CONFIGURATION
  const PAGE_MAP = {
    'index.html': {
      title: 'MediCare',
      subtitle: 'Smart Healthcare',
      icon: '+',
      navKey: 'overview',
    },
    'patient-dashboard.html': {
      title: 'MediCare',
      subtitle: 'Patient Daily Portal',
      icon: '+',
      navKey: 'routine',
    },
    'caregiver-dashboard.html': {
      title: 'MediCare',
      subtitle: 'Caregiver Console',
      icon: '+',
      navKey: 'caregiver',
    },
    'admin-dashboard.html': {
      title: 'MediCare',
      subtitle: 'Admin Console',
      icon: '+',
      navKey: 'admin',
    },
    'medicines.html': {
      title: 'MediCare',
      subtitle: 'Inventory & Routine',
      icon: '+',
      navKey: 'prescriptions',
    },
    'history.html': {
      title: 'MediCare',
      subtitle: 'Intake History',
      icon: '+',
      navKey: 'history',
    },
    'notifications.html': {
      title: 'MediCare',
      subtitle: 'Notifications',
      icon: '+',
      navKey: 'alerts',
    },
    'settings.html': {
      title: 'MediCare',
      subtitle: 'Account Settings',
      icon: '+',
      navKey: 'settings',
    },
    'profile-settings.html': {
      title: 'MediCare',
      subtitle: 'Profile Settings',
      icon: '+',
      navKey: 'settings',
    },
    'emergency.html': {
      title: 'MediCare',
      subtitle: 'SOS Command',
      icon: '🚨',
      navKey: 'emergency',
    },
    'ai-assistant.html': {
      title: 'MediCare',
      subtitle: 'AI Health Chat',
      icon: '🤖',
      navKey: 'ai',
    },
    'login.html': {
      title: 'MediCare',
      subtitle: 'Authentication Hub',
      icon: '+',
      navKey: 'auth',
    },
    'print-report.html': {
      title: 'MediCare',
      subtitle: 'Dossier Service',
      icon: '💙',
      navKey: 'report',
      isPrintReport: true,
    },
  };

  // 4. PUBLIC LINKS (Strictly visible BEFORE Login / Registration)
  const PUBLIC_NAV_LINKS = [
    { label: 'Overview', href: 'index.html', key: 'overview' },
    { label: 'Features', href: 'index.html#features', key: 'features' },
    { label: 'Live Charts', href: 'index.html#dashboard-overview', key: 'charts' },
  ];

  // 5. AUTHENTICATED ROLE-BASED PORTAL LINKS (Unlocked ONLY AFTER Login / Registration)
  const ROLE_NAV_LINKS = {
    PATIENT: [
      { label: 'Overview', href: 'index.html', key: 'overview' },
      { label: 'Routine', href: 'patient-dashboard.html', key: 'routine' },
      { label: 'Prescriptions', href: 'medicines.html', key: 'prescriptions' },
      { label: 'History', href: 'history.html', key: 'history' },
      { label: 'Alerts', href: 'notifications.html', key: 'alerts' },
      { label: 'Settings', href: 'settings.html', key: 'settings' },
    ],
    CAREGIVER: [
      { label: 'Overview', href: 'index.html', key: 'overview' },
      { label: 'Caregiver Hub', href: 'caregiver-dashboard.html', key: 'caregiver' },
      { label: 'Prescriptions', href: 'medicines.html', key: 'prescriptions' },
      { label: 'History', href: 'history.html', key: 'history' },
      { label: 'Alerts', href: 'notifications.html', key: 'alerts' },
      { label: 'Settings', href: 'settings.html', key: 'settings' },
    ],
    ADMIN: [
      { label: 'Overview', href: 'index.html', key: 'overview' },
      { label: 'Admin Console', href: 'admin-dashboard.html', key: 'admin' },
      { label: 'Prescriptions', href: 'medicines.html', key: 'prescriptions' },
      { label: 'History', href: 'history.html', key: 'history' },
      { label: 'Alerts', href: 'notifications.html', key: 'alerts' },
      { label: 'Settings', href: 'settings.html', key: 'settings' },
    ],
  };

  // 6. PUBLIC DRAWER LINKS (Before Login / Registration)
  const PUBLIC_DRAWER_LINKS = [
    { label: 'Home Overview', href: 'index.html', icon: '🏠', key: 'overview' },
    { label: 'Platform Features', href: 'index.html#features', icon: '⚡', key: 'features' },
    { label: 'Compliance Graphs', href: 'index.html#dashboard-overview', icon: '📊', key: 'charts' },
  ];

  // 7. AUTHENTICATED ROLE-BASED DRAWER LINKS (Unlocked AFTER Login / Registration)
  const ROLE_DRAWER_LINKS = {
    PATIENT: [
      { label: 'Home Overview', href: 'index.html', icon: '🏠', key: 'overview' },
      { label: 'Daily Routine', href: 'patient-dashboard.html', icon: '🕒', key: 'routine' },
      { label: 'Prescriptions', href: 'medicines.html', icon: '💊', key: 'prescriptions' },
      { label: 'Intake History', href: 'history.html', icon: '📋', key: 'history' },
      { label: 'Notification Alerts', href: 'notifications.html', icon: '🔔', key: 'alerts' },
      { label: 'Account Settings', href: 'settings.html', icon: '⚙️', key: 'settings' },
      { label: 'AI Health Assistant', href: 'ai-assistant.html', icon: '🤖', key: 'ai' },
      { label: 'Emergency SOS', href: 'emergency.html', icon: '🚨', key: 'emergency' },
    ],
    CAREGIVER: [
      { label: 'Home Overview', href: 'index.html', icon: '🏠', key: 'overview' },
      { label: 'Caregiver Hub', href: 'caregiver-dashboard.html', icon: '🛡️', key: 'caregiver' },
      { label: 'Prescriptions & Stock', href: 'medicines.html', icon: '💊', key: 'prescriptions' },
      { label: 'Patient History', href: 'history.html', icon: '📋', key: 'history' },
      { label: 'Critical Alerts', href: 'notifications.html', icon: '🔔', key: 'alerts' },
      { label: 'Account Settings', href: 'settings.html', icon: '⚙️', key: 'settings' },
      { label: 'Emergency SOS', href: 'emergency.html', icon: '🚨', key: 'emergency' },
    ],
    ADMIN: [
      { label: 'Home Overview', href: 'index.html', icon: '🏠', key: 'overview' },
      { label: 'Admin Console', href: 'admin-dashboard.html', icon: '💼', key: 'admin' },
      { label: 'Medicine Catalog', href: 'medicines.html', icon: '💊', key: 'prescriptions' },
      { label: 'Intake History', href: 'history.html', icon: '📋', key: 'history' },
      { label: 'System Alerts', href: 'notifications.html', icon: '🔔', key: 'alerts' },
      { label: 'Global Settings', href: 'settings.html', icon: '⚙️', key: 'settings' },
    ],
  };

  /**
   * Determine Current Page Filename
   */
  function getCurrentPageName() {
    const path = window.location.pathname.split('/').pop().toLowerCase();
    return path || 'index.html';
  }

  /**
   * Determine User Role and Dashboard Target with strict token validation
   */
  function getUserSession() {
    let role = null;
    let token = null;

    if (window.MediCareAPI) {
      token = window.MediCareAPI.getAuthToken();
      role = window.MediCareAPI.getUserRole();
    } else {
      token = localStorage.getItem('medicare_jwt_token') || localStorage.getItem('jwtToken') || localStorage.getItem('token');
      role = localStorage.getItem('medicare_role') || localStorage.getItem('userRole');
    }

    // Strict validation: token must exist and not be empty / null string
    const isValidToken = !!token && token !== 'null' && token !== 'undefined' && token.trim() !== '';

    if (!isValidToken) {
      return {
        isLoggedIn: false,
        role: null,
        dashboardUrl: 'login.html',
      };
    }

    role = role ? role.toUpperCase() : 'PATIENT';
    let dashboardUrl = 'patient-dashboard.html';
    if (role === 'CAREGIVER') dashboardUrl = 'caregiver-dashboard.html';
    else if (role === 'ADMIN') dashboardUrl = 'admin-dashboard.html';

    return {
      isLoggedIn: true,
      role,
      dashboardUrl,
    };
  }

  /**
   * Route Guard: Prevent unauthenticated users from opening protected links/pages
   */
  function enforceRouteGuard(session, currentPage) {
    if (PUBLIC_PAGES.includes(currentPage)) {
      return true;
    }

    // Unauthenticated user trying to open a protected page
    if (!session.isLoggedIn) {
      try {
        if (document.body) document.body.style.display = 'none';
        document.documentElement.style.display = 'none';
      } catch (e) {}

      alert('🔒 Access Restricted: Please sign in or register to access this MediCare portal page.');
      window.location.replace(`login.html?redirect=${encodeURIComponent(currentPage)}&auth_required=true`);
      return false;
    }

    // Role-based permission check for authenticated users
    const allowedRoles = ROUTE_PERMISSIONS[currentPage];
    if (allowedRoles && !allowedRoles.includes(session.role)) {
      try {
        if (document.body) document.body.style.display = 'none';
        document.documentElement.style.display = 'none';
      } catch (e) {}

      alert(`⚠️ Access Restricted: Your account (${session.role}) does not have permission for this portal.`);
      window.location.replace(session.dashboardUrl || 'index.html');
      return false;
    }

    return true;
  }

  /**
   * Build Header HTML Template
   */
  function buildHeaderHTML(meta, session, currentPage) {
    const isPrintPage = meta.isPrintReport || currentPage === 'print-report.html';

    // 1. Brand Section (Role badge only shown when logged in)
    const roleBadge = session.isLoggedIn && session.role
      ? `<span class="badge badge-role" style="margin-left: 6px;">${session.role}</span>`
      : '';

    const brandHTML = `
      <a href="index.html" class="brand-logo" title="MediCare Home">
        <div class="logo-icon">${meta.icon}</div>
        <div class="brand-text">
          <h1>${meta.title}</h1>
          <span>${meta.subtitle}</span>
        </div>
        ${roleBadge}
      </a>
    `;

    // 2. Desktop Links:
    // When LOGGED IN: show role-specific authenticated links
    // When NOT LOGGED IN: strictly show only public links (Overview, Features, Live Charts)
    let navLinksHTML = '';
    if (!isPrintPage && currentPage !== 'login.html') {
      let linksToShow = [];
      if (session.isLoggedIn) {
        linksToShow = ROLE_NAV_LINKS[session.role] || ROLE_NAV_LINKS.PATIENT;
      } else {
        linksToShow = PUBLIC_NAV_LINKS;
      }

      const linksItems = linksToShow.map((item) => {
        const isActive = item.href === currentPage || meta.navKey === item.key;
        return `<li><a href="${item.href}" class="${isActive ? 'active' : ''}">${item.label}</a></li>`;
      }).join('');

      navLinksHTML = `<ul class="desktop-nav-links">${linksItems}</ul>`;
    }

    // 3. Desktop Action Cluster
    let actionsHTML = '';
    if (isPrintPage) {
      actionsHTML = `
        <div class="desktop-actions no-print">
          <button class="btn btn-ghost" onclick="window.close(); history.back();">← Back</button>
          <button class="btn btn-primary" onclick="if(window.triggerNativePrint){window.triggerNativePrint();}else{window.print();}">🖨️ Print / Download PDF</button>
        </div>
      `;
    } else if (currentPage === 'login.html') {
      actionsHTML = `
        <div class="desktop-actions">
          <a href="index.html" class="btn btn-ghost">← Overview</a>
        </div>
      `;
    } else if (session.isLoggedIn) {
      // Unlocked actions after login: SOS, Dashboard, and Sign Out
      actionsHTML = `
        <div class="desktop-actions">
          <button class="btn btn-sos" id="headerSosBtn" onclick="window.handleGlobalSOS()">🚨 SOS</button>
          <a href="${session.dashboardUrl}" class="btn btn-ghost">Dashboard</a>
          <button class="btn btn-ghost" onclick="window.handleSignOut()">Sign Out</button>
        </div>
      `;
    } else {
      // Actions when NOT logged in: Sign In & Register buttons
      actionsHTML = `
        <div class="desktop-auth-actions">
          <button class="btn btn-ghost" onclick="window.openAuthOrLogin('login')">Sign In</button>
          <button class="btn btn-primary" onclick="window.openAuthOrLogin('register')">Get Started</button>
        </div>
      `;
    }

    // 4. Mobile Actions (Hamburger always, SOS only when logged in)
    const mobileActionsHTML = isPrintPage
      ? ''
      : `
      <div class="mobile-actions">
        ${session.isLoggedIn ? '<button class="btn btn-sos" style="padding: 7px 12px; font-size: 12px;" onclick="window.handleGlobalSOS()">🚨 SOS</button>' : ''}
        <button class="hamburger-btn" id="menuToggle" aria-label="Toggle Navigation Menu">
          <span></span>
          <span></span>
          <span></span>
        </button>
      </div>
    `;

    return `
      <div class="nav-container">
        ${brandHTML}
        ${navLinksHTML}
        ${actionsHTML}
        ${mobileActionsHTML}
      </div>
    `;
  }

  /**
   * Build Mobile Drawer HTML Template
   */
  function buildDrawerHTML(meta, session, currentPage) {
    const isPrintPage = meta.isPrintReport || currentPage === 'print-report.html';
    if (isPrintPage) return '';

    // Logic: Only show full portal links in drawer if logged in; otherwise show only public links
    const drawerLinksToShow = session.isLoggedIn
      ? (ROLE_DRAWER_LINKS[session.role] || ROLE_DRAWER_LINKS.PATIENT)
      : PUBLIC_DRAWER_LINKS;

    const drawerItems = drawerLinksToShow.map((item) => {
      const isActive = item.href === currentPage || meta.navKey === item.key;
      return `
        <li>
          <a href="${item.href}" class="${isActive ? 'active' : ''}">
            <span style="font-size: 18px;">${item.icon}</span>
            <span>${item.label}</span>
          </a>
        </li>
      `;
    }).join('');

    let drawerAuthHTML = '';
    if (session.isLoggedIn) {
      drawerAuthHTML = `
        <div class="drawer-auth">
          <div style="font-size: 13px; color: var(--text-muted); margin-bottom: 4px;">
            Account: <strong>${session.role || 'User'}</strong>
          </div>
          <button class="btn btn-sos" style="width: 100%;" onclick="window.handleGlobalSOS()">
            🚨 EMERGENCY SOS
          </button>
          <a href="${session.dashboardUrl}" class="btn btn-primary" style="width: 100%;">
            👤 My Dashboard
          </a>
          <button class="btn btn-ghost" style="width: 100%;" onclick="window.handleSignOut()">
            Sign Out
          </button>
        </div>
      `;
    } else {
      drawerAuthHTML = `
        <div class="drawer-auth">
          <div style="font-size: 12.5px; color: var(--text-muted); text-align: center; margin-bottom: 6px;">
            Sign in or register to unlock your personal health portal.
          </div>
          <button class="btn btn-primary" style="width: 100%;" onclick="window.openAuthOrLogin('register'); window.toggleMenu();">
            Create Account
          </button>
          <button class="btn btn-ghost" style="width: 100%;" onclick="window.openAuthOrLogin('login'); window.toggleMenu();">
            Sign In
          </button>
        </div>
      `;
    }

    return `
      <div class="mobile-overlay" id="mobileOverlay"></div>
      <aside class="mobile-drawer" id="mobileDrawer" aria-label="Mobile Navigation Drawer">
        <div class="drawer-header">
          <div class="brand-logo">
            <div class="logo-icon">${meta.icon}</div>
            <div class="brand-text">
              <h1 style="font-size: 18px;">${meta.title}</h1>
            </div>
          </div>
          <button class="close-btn" id="closeDrawer" aria-label="Close Navigation Drawer">&times;</button>
        </div>
        <ul class="drawer-links">
          ${drawerItems}
        </ul>
        ${drawerAuthHTML}
      </aside>
    `;
  }

  // 8. DRAWER CONTROLLER FUNCTIONS
  function toggleMenu() {
    const drawer = document.getElementById('mobileDrawer');
    const overlay = document.getElementById('mobileOverlay');
    if (!drawer) return;

    const isOpen = drawer.classList.contains('active');
    if (isOpen) {
      closeDrawer();
    } else {
      openDrawer();
    }
  }

  function openDrawer() {
    const drawer = document.getElementById('mobileDrawer');
    const overlay = document.getElementById('mobileOverlay');
    if (!drawer) return;

    drawer.classList.add('active');
    if (overlay) overlay.classList.add('active');
    document.body.classList.add('drawer-open');
    document.body.style.overflow = 'hidden';
  }

  function closeDrawer() {
    const drawer = document.getElementById('mobileDrawer');
    const overlay = document.getElementById('mobileOverlay');
    if (!drawer) return;

    drawer.classList.remove('active');
    if (overlay) overlay.classList.remove('active');
    document.body.classList.remove('drawer-open');
    document.body.style.overflow = '';
  }

  // 9. ACTION HANDLERS
  function handleGlobalSOS() {
    if (typeof window.triggerPatientSOS === 'function') {
      window.triggerPatientSOS();
      return;
    }
    if (typeof window.triggerCaregiverEmergency === 'function') {
      window.triggerCaregiverEmergency();
      return;
    }
    if (typeof window.triggerPanicBroadcast === 'function') {
      window.triggerPanicBroadcast();
      return;
    }
    if (typeof window.triggerSOS === 'function') {
      window.triggerSOS();
      return;
    }

    const currentPage = getCurrentPageName();
    if (currentPage !== 'emergency.html') {
      window.location.href = 'emergency.html';
    } else {
      alert('🚨 EMERGENCY SOS: Immediate alert dispatched to linked caregivers!');
    }
  }

  function handleSignOut() {
    if (confirm('Are you sure you want to sign out of MediCare?')) {
      if (window.MediCareAPI && window.MediCareAPI.clearAuthToken) {
        window.MediCareAPI.clearAuthToken();
      } else {
        localStorage.removeItem('medicare_jwt_token');
        localStorage.removeItem('jwtToken');
        localStorage.removeItem('token');
        localStorage.removeItem('medicare_role');
        localStorage.removeItem('userRole');
        localStorage.removeItem('medicare_user');
      }
      // Return to homepage with logged-out public view
      window.location.href = 'index.html';
    }
  }

  function openAuthOrLogin(mode) {
    if (typeof window.openAuthModal === 'function') {
      window.openAuthModal(mode);
    } else {
      window.location.href = `login.html?mode=${mode}`;
    }
  }

  // 10. PUBLIC PAGE CLICK INTERCEPTOR
  // Intercept any click pointing to a protected portal page when user is not logged in
  function attachPublicLinkInterceptors() {
    document.addEventListener(
      'click',
      function (e) {
        const link = e.target.closest('a');
        if (!link) return;
        const href = link.getAttribute('href');
        if (!href || href.startsWith('#') || href.startsWith('javascript:') || href.startsWith('mailto:') || href.startsWith('tel:')) {
          return;
        }

        const targetPage = href.split('/').pop().split('?')[0].split('#')[0].toLowerCase();
        if (targetPage && targetPage.endsWith('.html') && !PUBLIC_PAGES.includes(targetPage)) {
          const session = getUserSession();
          if (!session.isLoggedIn) {
            e.preventDefault();
            e.stopPropagation();
            const doAuth = confirm(
              '🔒 Login Required\n\nThis section requires an active MediCare account.\n\nWould you like to Sign In or Create an Account now?'
            );
            if (doAuth) {
              openAuthOrLogin('login');
            }
          }
        }
      },
      true
    );
  }

  // 11. ATTACH DRAWER LISTENERS
  function attachDrawerListeners() {
    const menuToggle = document.getElementById('menuToggle');
    const closeDrawerBtn = document.getElementById('closeDrawer');
    const overlay = document.getElementById('mobileOverlay');

    if (menuToggle) {
      menuToggle.removeEventListener('click', toggleMenu);
      menuToggle.addEventListener('click', toggleMenu);
    }

    if (closeDrawerBtn) {
      closeDrawerBtn.removeEventListener('click', closeDrawer);
      closeDrawerBtn.addEventListener('click', closeDrawer);
    }

    if (overlay) {
      overlay.removeEventListener('click', closeDrawer);
      overlay.addEventListener('click', closeDrawer);
    }

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') closeDrawer();
    });

    window.addEventListener('resize', () => {
      if (window.innerWidth > 920) closeDrawer();
    });
  }

  // 12. INITIALIZATION & DOM INJECTION
  function initNavbar() {
    const currentPage = getCurrentPageName();
    const session = getUserSession();

    // 1. Strict route guard check
    if (!enforceRouteGuard(session, currentPage)) {
      return;
    }

    // 2. Locate or create header mount element
    let headerMount = document.getElementById('global-header');
    if (!headerMount) {
      headerMount = document.querySelector('.site-header, .top-bar, .web-header');
    }
    if (!headerMount) {
      headerMount = document.createElement('header');
      headerMount.id = 'global-header';
      headerMount.className = 'site-header';
      document.body.prepend(headerMount);
    }

    headerMount.classList.add('site-header');

    if (currentPage === 'print-report.html') {
      headerMount.classList.add('no-print');
    }

    // 3. Resolve Page Metadata
    const customSubtitle = headerMount.getAttribute('data-subtitle');
    const customIcon = headerMount.getAttribute('data-icon');
    const customNavKey = headerMount.getAttribute('data-nav');

    const meta = {
      ...(PAGE_MAP[currentPage] || {
        title: 'MediCare',
        subtitle: 'Healthcare Platform',
        icon: '+',
        navKey: '',
      }),
    };

    if (customSubtitle) meta.subtitle = customSubtitle;
    if (customIcon) meta.icon = customIcon;
    if (customNavKey) meta.navKey = customNavKey;

    // 4. Render Header
    headerMount.innerHTML = buildHeaderHTML(meta, session, currentPage);

    // 5. Remove any previous dynamic drawer elements to avoid duplicates
    const oldOverlay = document.getElementById('mobileOverlay');
    const oldDrawer = document.getElementById('mobileDrawer');
    if (oldOverlay) oldOverlay.remove();
    if (oldDrawer) oldDrawer.remove();

    // 6. Render Drawer right after Header
    if (currentPage !== 'print-report.html') {
      const drawerFragment = document.createRange().createContextualFragment(
        buildDrawerHTML(meta, session, currentPage)
      );
      headerMount.parentNode.insertBefore(drawerFragment, headerMount.nextSibling);
    }

    // 7. Bind interactive listeners
    attachDrawerListeners();
  }

  // 13. EXPORT GLOBALS & AUTO-RUN
  window.toggleMenu = toggleMenu;
  window.openDrawer = openDrawer;
  window.closeDrawer = closeDrawer;
  window.handleGlobalSOS = handleGlobalSOS;
  window.handleSignOut = handleSignOut;
  window.openAuthOrLogin = openAuthOrLogin;
  window.initMediCareNavbar = initNavbar;
  window.getUserSession = getUserSession;

  // Attach link click interceptor on public pages
  attachPublicLinkInterceptors();

  // Run on DOM ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initNavbar);
  } else {
    initNavbar();
  }

  // Reactive listeners: instantly unlock or hide links when user logs in, registers, or logs out
  window.addEventListener('medicare:auth-change', initNavbar);
  window.addEventListener('storage', (e) => {
    if (!e.key || e.key.includes('medicare') || e.key.includes('token') || e.key.includes('Role')) {
      initNavbar();
    }
  });

})(window, document);
