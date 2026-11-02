/**
 * MEDICARE PLATFORM - MODULAR HEADER & MOBILE NAVIGATION DRAWER
 * File: js/navbar.js
 * Description: Client-side dynamic injection for the unified navigation header,
 *              responsive action cluster, dynamic role indicators, slide-in drawer,
 *              strict route guards, and unauthenticated access prevention.
 */

(function (window, document) {
  "use strict";

  // 0. EARLY AUTH GUARD: Protect dashboard pages immediately so unauthenticated users cannot view them
  (function earlyAuthGuard() {
    const page =
      window.location.pathname.split("/").pop().toLowerCase() || "index.html";
    const publicPages = ["index.html", "login.html", ""];
    if (publicPages.includes(page)) return;

    let token = null;
    try {
      token =
        localStorage.getItem("medicare_jwt_token") ||
        localStorage.getItem("jwtToken") ||
        localStorage.getItem("token");
    } catch (e) {}

    const isValid =
      !!token &&
      token !== "null" &&
      token !== "undefined" &&
      token.trim() !== "";
    if (!isValid) {
      if (document.documentElement)
        document.documentElement.style.display = "none";
      if (document.body) document.body.style.display = "none";
      window.location.replace(
        `login.html?redirect=${encodeURIComponent(page)}&auth_required=true`,
      );
    }
  })();

  // 1. PUBLIC PAGES (Always accessible without login or registration)
  const PUBLIC_PAGES = [
    "index.html",
    "login.html",
    "register.html",
    "registration.html",
    "",
  ];

  // 2. PROTECTED ROUTES & ROLE PERMISSIONS
  const ROUTE_PERMISSIONS = {
    "patient-dashboard.html": ["PATIENT", "ADMIN"],
    "caregiver-dashboard.html": ["CAREGIVER", "ADMIN"],
    "admin-dashboard.html": ["ADMIN"],
    "schedule.html": ["PATIENT", "CAREGIVER", "ADMIN"],
    "medicines.html": ["PATIENT", "CAREGIVER", "ADMIN"],
    "history.html": ["PATIENT", "CAREGIVER", "ADMIN"],
    "notifications.html": ["PATIENT", "CAREGIVER", "ADMIN"],
    "my-caregivers.html": ["PATIENT", "ADMIN"],
    "settings.html": ["PATIENT", "CAREGIVER", "ADMIN"],
    "profile-settings.html": ["PATIENT", "CAREGIVER", "ADMIN"],
    "emergency.html": ["PATIENT"],
    "ai-assistant.html": ["PATIENT", "CAREGIVER", "ADMIN"],
    "print-report.html": ["PATIENT", "CAREGIVER", "ADMIN"],
  };

  // 3. PAGE METADATA CONFIGURATION
  const PAGE_MAP = {
    "my-caregivers.html": {
      title: "MediCare",
      subtitle: "My Caregivers",
      icon: "🩺",
      navKey: "caregivers",
    },
    "index.html": {
      title: "MediCare",
      subtitle: "Smart Healthcare",
      icon: "+",
      navKey: "overview",
    },
    "patient-dashboard.html": {
      title: "MediCare",
      subtitle: "Patient Daily Portal",
      icon: "+",
      navKey: "routine",
    },
    "caregiver-dashboard.html": {
      title: "MediCare",
      subtitle: "Caregiver Console",
      icon: "+",
      navKey: "caregiver",
    },
    "admin-dashboard.html": {
      title: "MediCare",
      subtitle: "Admin Console",
      icon: "+",
      navKey: "admin",
    },
    "medicines.html": {
      title: "MediCare",
      subtitle: "Inventory & Routine",
      icon: "+",
      navKey: "Add Medicine",
    },
    "schedule.html": {
      title: "MediCare",
      subtitle: "Medicine Schedule",
      icon: "📅",
      navKey: "schedule",
    },
    "history.html": {
      title: "MediCare",
      subtitle: "Intake History",
      icon: "+",
      navKey: "history",
    },
    "notifications.html": {
      title: "MediCare",
      subtitle: "Notifications",
      icon: "+",
      navKey: "alerts",
    },
    "settings.html": {
      title: "MediCare",
      subtitle: "Account Settings",
      icon: "+",
      navKey: "settings",
    },
    "profile-settings.html": {
      title: "MediCare",
      subtitle: "Profile Settings",
      icon: "+",
      navKey: "settings",
    },
    "emergency.html": {
      title: "MediCare",
      subtitle: "SOS Command",
      icon: "🚨",
      navKey: "emergency",
    },
    "ai-assistant.html": {
      title: "MediCare",
      subtitle: "AI Health Chat",
      icon: "🤖",
      navKey: "ai",
    },
    "login.html": {
      title: "MediCare",
      subtitle: "Authentication Hub",
      icon: "+",
      navKey: "auth",
    },
    "register.html": {
      title: "MediCare",
      subtitle: "Authentication Hub",
      icon: "+",
      navKey: "auth",
    },
    "registration.html": {
      title: "MediCare",
      subtitle: "Authentication Hub",
      icon: "+",
      navKey: "auth",
    },
    "print-report.html": {
      title: "MediCare",
      subtitle: "Dossier Service",
      icon: "💙",
      navKey: "report",
      isPrintReport: true,
    },
  };

  // 4. PUBLIC LINKS (Strictly visible BEFORE Login / Registration)
  const PUBLIC_NAV_LINKS = [
    { label: "Overview", href: "index.html", key: "overview" },
    { label: "Features", href: "index.html#features", key: "features" },
    {
      label: "Live Charts",
      href: "index.html#dashboard-overview",
      key: "charts",
    },
  ];

  // 5. AUTHENTICATED ROLE-BASED PORTAL LINKS (Unlocked ONLY AFTER Login / Registration)
  const ROLE_NAV_LINKS = {
    PATIENT: [
      {
        label: "Today's Routine",
        href: "patient-dashboard.html",
        key: "routine",
      },
      { label: "My Schedule", href: "schedule.html", key: "schedule" },
      { label: "Medicine List", href: "medicines.html", key: "medicines" },
      { label: "History", href: "history.html", key: "history" },
      { label: "My Caregivers", href: "my-caregivers.html", key: "caregivers" },
      { label: "Emergency & Help", href: "emergency.html", key: "emergency" },
      { label: "Alerts", href: "notifications.html", key: "alerts" },
      { label: "Settings", href: "settings.html", key: "settings" },
    ],
    CAREGIVER: [
      {
        label: "Patients Hub",
        href: "caregiver-dashboard.html",
        key: "caregiver",
      },
      { label: "Schedules", href: "schedule.html", key: "schedule" },
      { label: "Medicine Catalog", href: "medicines.html", key: "medicines" },
      { label: "Intake History", href: "history.html", key: "history" },
      { label: "Alerts", href: "notifications.html", key: "alerts" },
      { label: "Settings", href: "settings.html", key: "settings" },
    ],
    ADMIN: [
      { label: "Admin Console", href: "admin-dashboard.html", key: "admin" },
      { label: "Schedules", href: "schedule.html", key: "schedule" },
      { label: "Medicines", href: "medicines.html", key: "medicines" },
      { label: "History", href: "history.html", key: "history" },
      { label: "Alerts", href: "notifications.html", key: "alerts" },
      { label: "Settings", href: "settings.html", key: "settings" },
    ],
  };

  // 6. PUBLIC DRAWER LINKS (Before Login / Registration)
  const PUBLIC_DRAWER_LINKS = [
    { label: "Home Overview", href: "index.html", icon: "🏠", key: "overview" },
    {
      label: "Features",
      href: "index.html#features",
      icon: "✨",
      key: "features",
    },
    {
      label: "Adherence Graphs",
      href: "index.html#dashboard-overview",
      icon: "📊",
      key: "charts",
    },
  ];

  // 7. AUTHENTICATED ROLE-BASED DRAWER LINKS (Unlocked AFTER Login / Registration)
  const ROLE_DRAWER_LINKS = {
    PATIENT: [
      {
        label: "Today's Routine",
        href: "patient-dashboard.html",
        icon: "🕒",
        key: "routine",
      },
      {
        label: "My Schedule",
        href: "schedule.html",
        icon: "📅",
        key: "schedule",
      },
      {
        label: "Medicine List",
        href: "medicines.html",
        icon: "💊",
        key: "medicines",
      },
      {
        label: "Intake History",
        href: "history.html",
        icon: "📋",
        key: "history",
      },
      {
        label: "Emergency & Help",
        href: "emergency.html",
        icon: "🚨",
        key: "emergency",
      },
      {
        label: "Notifications",
        href: "notifications.html",
        icon: "🔔",
        key: "alerts",
      },
      {
        label: "AI Health Assistant",
        href: "ai-assistant.html",
        icon: "🤖",
        key: "ai",
      },
      { label: "Settings", href: "settings.html", icon: "⚙️", key: "settings" },
    ],
    CAREGIVER: [
      {
        label: "Patients Hub",
        href: "caregiver-dashboard.html",
        icon: "🛡️",
        key: "caregiver",
      },
      {
        label: "Medicine Schedules",
        href: "schedule.html",
        icon: "📅",
        key: "schedule",
      },
      {
        label: "Medicine Catalog",
        href: "medicines.html",
        icon: "💊",
        key: "medicines",
      },
      {
        label: "Patient History",
        href: "history.html",
        icon: "📋",
        key: "history",
      },
      {
        label: "Critical Alerts",
        href: "notifications.html",
        icon: "🔔",
        key: "alerts",
      },
      {
        label: "AI Health Assistant",
        href: "ai-assistant.html",
        icon: "🤖",
        key: "ai",
      },
      {
        label: "Account Settings",
        href: "settings.html",
        icon: "⚙️",
        key: "settings",
      },
    ],
    ADMIN: [
      {
        label: "Admin Console",
        href: "admin-dashboard.html",
        icon: "💼",
        key: "admin",
      },
      {
        label: "Schedules",
        href: "schedule.html",
        icon: "📅",
        key: "schedule",
      },
      {
        label: "Medicine Catalog",
        href: "medicines.html",
        icon: "💊",
        key: "medicines",
      },
      {
        label: "Intake History",
        href: "history.html",
        icon: "📋",
        key: "history",
      },
      {
        label: "System Alerts",
        href: "notifications.html",
        icon: "🔔",
        key: "alerts",
      },
      {
        label: "Global Settings",
        href: "settings.html",
        icon: "⚙️",
        key: "settings",
      },
    ],
  };

  /**
   * Determine Current Page Filename
   */
  function getCurrentPageName() {
    const path = window.location.pathname.split("/").pop().toLowerCase();
    return path || "index.html";
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
      token =
        localStorage.getItem("medicare_jwt_token") ||
        localStorage.getItem("jwtToken") ||
        localStorage.getItem("token");
      role =
        localStorage.getItem("medicare_role") ||
        localStorage.getItem("userRole");
    }

    // Strict validation: token must exist and not be empty / null string
    const isValidToken =
      !!token &&
      token !== "null" &&
      token !== "undefined" &&
      token.trim() !== "";

    if (!isValidToken) {
      return {
        isLoggedIn: false,
        role: null,
        dashboardUrl: "login.html",
      };
    }

    role = role ? role.toUpperCase() : "PATIENT";
    let dashboardUrl = "patient-dashboard.html";
    if (role === "CAREGIVER") dashboardUrl = "caregiver-dashboard.html";
    else if (role === "ADMIN") dashboardUrl = "admin-dashboard.html";

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
        if (document.body) document.body.style.display = "none";
        document.documentElement.style.display = "none";
      } catch (e) {}

      alert(
        "🔒 Access Restricted: Please sign in or register to access this MediCare portal page.",
      );
      window.location.replace(
        `login.html?redirect=${encodeURIComponent(currentPage)}&auth_required=true`,
      );
      return false;
    }

    // Role-based permission check for authenticated users
    const allowedRoles = ROUTE_PERMISSIONS[currentPage];
    if (allowedRoles && !allowedRoles.includes(session.role)) {
      try {
        if (document.body) document.body.style.display = "none";
        document.documentElement.style.display = "none";
      } catch (e) {}

      alert(
        `⚠️ Access Restricted: Your account (${session.role}) does not have permission for this portal.`,
      );
      window.location.replace(session.dashboardUrl || "index.html");
      return false;
    }

    return true;
  }

  // Medical Cross SVG Icon for professional healthcare branding matching reference image
  const DEFAULT_MED_ICON = `
    <svg class="med-cross-icon" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="#ffffff" stroke-width="4.5" stroke-linecap="round" stroke-linejoin="round">
      <line x1="12" y1="4.5" x2="12" y2="19.5"></line>
      <line x1="4.5" y1="12" x2="19.5" y2="12"></line>
    </svg>
  `;

  /**
   * Build Header HTML Template
   */
  function buildHeaderHTML(meta, session, currentPage) {
    const isPrintPage =
      meta.isPrintReport || currentPage === "print-report.html";

    // 1. AUTHENTICATED PAGES (After Login)
    // Strictly: MediCare logo on LEFT, 3-line hamburger button on RIGHT, NO extra buttons
    if (
      session.isLoggedIn &&
      currentPage !== "login.html" &&
      currentPage !== "index.html" &&
      currentPage !== ""
    ) {
      const targetDashboard = session.dashboardUrl || "patient-dashboard.html";
      const roleStr = (session.role || "PATIENT").toUpperCase();

      return `
        <div class="nav-container authenticated-nav-container">
          <a href="${targetDashboard}" class="brand-logo" title="MediCare Dashboard">
            <div class="logo-icon">${DEFAULT_MED_ICON}</div>
            <div class="brand-text">
              <h1><span class="brand-medi">Medi</span><span class="brand-care">Care</span></h1>
            </div>
            <span class="badge badge-role role-${roleStr.toLowerCase()}"><span class="role-status-dot"></span>${roleStr}</span>
          </a>
          <div class="header-right-cluster">
            <button class="hamburger-btn" id="menuToggle" aria-label="Toggle Navigation Menu" title="Navigation Menu">
              <span></span>
              <span></span>
              <span></span>
            </button>
          </div>
        </div>
      `;
    }

    // 2. PUBLIC & AUTH PAGES (index.html, login.html, register.html, registration.html)
    // Strictly unified, identical header across homepage and login/registration pages
    const brandHTML = `
      <a href="index.html" class="brand-logo" title="MediCare Home">
        <div class="logo-icon">${DEFAULT_MED_ICON}</div>
        <div class="brand-text">
          <h1><span class="brand-medi">Medi</span><span class="brand-care">Care</span></h1>
        </div>
      </a>
    `;

    const linksItems = PUBLIC_NAV_LINKS.map((item) => {
      const isActive = item.href === currentPage || meta.navKey === item.key;
      return `<li><a href="${item.href}" class="${isActive ? "active" : ""}">${item.label}</a></li>`;
    }).join("");

    const navLinksHTML = `<ul class="desktop-nav-links">${linksItems}</ul>`;

    const isNoHamburgerPage =
      currentPage === "index.html" ||
      currentPage === "" ||
      currentPage === "login.html" ||
      currentPage === "register.html" ||
      currentPage === "registration.html";

    const hamburgerHTML = isNoHamburgerPage
      ? ""
      : `
          <button class="hamburger-btn" id="menuToggle" aria-label="Toggle Navigation Menu" title="Navigation Menu">
            <span></span>
            <span></span>
            <span></span>
          </button>
        `;

    return `
      <div class="nav-container">
        ${brandHTML}
        ${navLinksHTML}
        <div class="header-right-cluster">
          <button class="btn btn-ghost header-signin-btn" onclick="window.openAuthOrLogin('login')">Sign In</button>
          ${hamburgerHTML}
        </div>
      </div>
    `;
  }

  /**
   * Build Mobile Drawer HTML Template
   */
  function buildDrawerHTML(meta, session, currentPage) {
    const isPrintPage =
      meta.isPrintReport || currentPage === "print-report.html";
    const isAuthPage =
      currentPage === "login.html" ||
      currentPage === "register.html" ||
      currentPage === "registration.html";
    if (isPrintPage || isAuthPage) return "";

    // Logic: Only show full portal links in drawer if logged in; otherwise show only public links
    const drawerLinksToShow = session.isLoggedIn
      ? ROLE_DRAWER_LINKS[session.role] || ROLE_DRAWER_LINKS.PATIENT
      : PUBLIC_DRAWER_LINKS;

    const drawerItems = drawerLinksToShow
      .map((item) => {
        const isActive = item.href === currentPage || meta.navKey === item.key;
        return `
        <li>
          <a href="${item.href}" class="${isActive ? "active" : ""}">
            <span class="drawer-link-icon">${item.icon}</span>
            <span>${item.label}</span>
          </a>
        </li>
      `;
      })
      .join("");

    const iconContent =
      meta.icon === "+" || !meta.icon ? DEFAULT_MED_ICON : meta.icon;

    let drawerAuthHTML = "";
    if (session.isLoggedIn) {
      const isPatientRole = (session.role || "").toUpperCase() === "PATIENT";
      drawerAuthHTML = `
        <div class="drawer-auth">
          <div style="font-size: 13px; color: var(--text-muted); margin-bottom: 6px; display: flex; align-items: center; justify-content: space-between;">
            <span>Connected Role:</span>
            <span class="badge badge-role role-${(session.role || "user").toLowerCase()}"><span class="role-status-dot"></span>${session.role || "User"}</span>
          </div>
          <button class="btn btn-help" style="width: 100%; margin-bottom: 8px;" onclick="window.openHelpModal(); window.closeDrawer();">
            ❓ Help & Simple Guide
          </button>
          ${
            isPatientRole
              ? `
          <button class="btn btn-sos" style="width: 100%; margin-bottom: 8px;" onclick="window.handleGlobalSOS()">
            🚨 EMERGENCY SOS (Tap For Help)
          </button>`
              : ""
          }
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
          <div style="font-size: 13px; color: var(--text-muted); text-align: center; margin-bottom: 8px;">
            Sign in or create an account to view your medicine schedule.
          </div>
          <button class="btn btn-help" style="width: 100%; margin-bottom: 8px;" onclick="window.openHelpModal(); window.closeDrawer();">
            ❓ Simple Help Guide
          </button>
          <button class="btn btn-primary" style="width: 100%;" onclick="window.openAuthOrLogin('register'); window.toggleMenu();">
            Create Account →
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
            <div class="logo-icon">${DEFAULT_MED_ICON}</div>
            <div class="brand-text">
              <h1 style="font-size: 18px;">Medi<span class="brand-accent">Care</span></h1>
              <span class="brand-subtitle">Smart Healthcare</span>
            </div>
          </div>
          <button class="close-btn" id="closeDrawer" aria-label="Close Navigation Drawer">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>
        <ul class="drawer-links">
          ${drawerItems}
        </ul>
        ${drawerAuthHTML}
      </aside>
    `;
  }

  // 8. DRAWER & SIDEBAR CONTROLLER FUNCTIONS
  function toggleMenu() {
    const drawer = document.getElementById("mobileDrawer");
    if (!drawer) {
      toggleSidebar();
      return;
    }

    const isOpen = drawer.classList.contains("active");
    if (isOpen) {
      closeDrawer();
    } else {
      openDrawer();
    }
  }

  function openDrawer() {
    const drawer = document.getElementById("mobileDrawer");
    const overlay = document.getElementById("mobileOverlay");
    const menuToggle = document.getElementById("menuToggle");
    if (!drawer) return;

    drawer.classList.add("active");
    if (overlay) overlay.classList.add("active");
    if (menuToggle) menuToggle.classList.add("active");
    document.body.classList.add("drawer-open");
    document.body.style.overflow = "hidden";
  }

  function closeDrawer() {
    const drawer = document.getElementById("mobileDrawer");
    const overlay = document.getElementById("mobileOverlay");
    const menuToggle = document.getElementById("menuToggle");
    if (!drawer) return;

    drawer.classList.remove("active");
    if (overlay) overlay.classList.remove("active");
    if (menuToggle) menuToggle.classList.remove("active");
    document.body.classList.remove("drawer-open");
    document.body.style.overflow = "";
  }

  function openSidebar() {
    const sb = document.getElementById("medicareSidebar");
    const bd = document.getElementById("sidebarBackdrop");
    if (sb) sb.classList.add("open");
    if (bd) bd.classList.add("active");
    document.body.style.overflow = "hidden";
  }

  function closeSidebar() {
    const sb = document.getElementById("medicareSidebar");
    const bd = document.getElementById("sidebarBackdrop");
    if (sb) sb.classList.remove("open");
    if (bd) bd.classList.remove("active");
    document.body.style.overflow = "";
  }

  function toggleSidebar() {
    const sb = document.getElementById("medicareSidebar");
    if (sb && sb.classList.contains("open")) {
      closeSidebar();
    } else {
      openSidebar();
    }
  }

  window.openSidebar = openSidebar;
  window.closeSidebar = closeSidebar;
  window.toggleSidebar = toggleSidebar;

  // 9. ACTION HANDLERS
  async function handleGlobalSOS() {
    // 1. Explicit user confirmation before sending emergency alert
    const confirmSOS = confirm(
      "🚨 EMERGENCY SOS CONFIRMATION\n\nAre you sure you want to trigger an Emergency SOS Alert?\n\nThis will immediately notify your linked caregivers and record an active emergency incident.",
    );
    if (!confirmSOS) return;

    if (typeof window.triggerPatientSOS === "function") {
      window.triggerPatientSOS(true);
      return;
    }
    if (typeof window.triggerCaregiverEmergency === "function") {
      window.triggerCaregiverEmergency(true);
      return;
    }
    if (typeof window.triggerPanicBroadcast === "function") {
      window.triggerPanicBroadcast(true);
      return;
    }

    // Direct backend integration: call /api/emergency
    try {
      if (window.MediCareAPI && window.MediCareAPI.apiFetch) {
        await window.MediCareAPI.apiFetch("/emergency", "POST", {
          message: "Emergency SOS Beacon Triggered from Navigation Header",
        });
        alert(
          "🚨 EMERGENCY SOS SENT!\n\nYour alert has been saved to the database and your linked caregivers have been notified.",
        );
      } else {
        const token = localStorage.getItem("medicare_jwt_token");
        const apiBase =
          (window.MediCareAPI && window.MediCareAPI.API_BASE_URL) ||
          "http://localhost:5000/api";
        const res = await fetch(`${apiBase}/emergency`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({
            message: "Emergency SOS Beacon Triggered from Navigation Header",
          }),
        });
        if (!res.ok) throw new Error(`HTTP error ${res.status}`);
        alert(
          "🚨 EMERGENCY SOS SENT!\n\nYour emergency alert was sent to the server.",
        );
      }
    } catch (err) {
      console.error("[Emergency SOS Error]", err);
      // NEVER pretend an alert was sent if the request fails
      alert(
        "⚠️ EMERGENCY ALERT COULD NOT BE SENT: " +
          (err.message || "Cannot reach server.") +
          "\n\nPlease call emergency services (911 / 112 / 108) or phone your caregiver directly right now!",
      );
    }
  }

  function handleSignOut() {
    if (confirm("Are you sure you want to sign out of MediCare?")) {
      if (window.MediCareAPI && window.MediCareAPI.clearAuthToken) {
        window.MediCareAPI.clearAuthToken();
      } else {
        localStorage.removeItem("medicare_jwt_token");
        localStorage.removeItem("jwtToken");
        localStorage.removeItem("token");
        localStorage.removeItem("medicare_role");
        localStorage.removeItem("userRole");
        localStorage.removeItem("medicare_user");
      }
      // Return to homepage with logged-out public view
      window.location.href = "index.html";
    }
  }

  function openAuthOrLogin(mode) {
    if (typeof window.switchMode === "function") {
      window.switchMode(mode);
    } else if (typeof window.openAuthModal === "function") {
      window.openAuthModal(mode);
    } else {
      window.location.href = `login.html?mode=${mode}`;
    }
  }

  // 10. PUBLIC PAGE CLICK INTERCEPTOR
  // Intercept any click pointing to a protected portal page when user is not logged in
  function attachPublicLinkInterceptors() {
    document.addEventListener(
      "click",
      function (e) {
        const link = e.target.closest("a");
        if (!link) return;
        const href = link.getAttribute("href");
        if (
          !href ||
          href.startsWith("#") ||
          href.startsWith("javascript:") ||
          href.startsWith("mailto:") ||
          href.startsWith("tel:")
        ) {
          return;
        }

        const targetPage = href
          .split("/")
          .pop()
          .split("?")[0]
          .split("#")[0]
          .toLowerCase();
        if (
          targetPage &&
          targetPage.endsWith(".html") &&
          !PUBLIC_PAGES.includes(targetPage)
        ) {
          const session = getUserSession();
          if (!session.isLoggedIn) {
            e.preventDefault();
            e.stopPropagation();
            const doAuth = confirm(
              "🔒 Login Required\n\nThis section requires an active MediCare account.\n\nWould you like to Sign In or Create an Account now?",
            );
            if (doAuth) {
              openAuthOrLogin("login");
            }
          }
        }
      },
      true,
    );
  }

  // 11. ATTACH DRAWER LISTENERS
  function attachDrawerListeners() {
    const menuToggle = document.getElementById("menuToggle");
    const closeDrawerBtn = document.getElementById("closeDrawer");
    const overlay = document.getElementById("mobileOverlay");

    if (menuToggle) {
      menuToggle.removeEventListener("click", toggleMenu);
      menuToggle.addEventListener("click", toggleMenu);
    }

    if (closeDrawerBtn) {
      closeDrawerBtn.removeEventListener("click", closeDrawer);
      closeDrawerBtn.addEventListener("click", closeDrawer);
    }

    if (overlay) {
      overlay.removeEventListener("click", closeDrawer);
      overlay.addEventListener("click", closeDrawer);
    }

    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape") closeDrawer();
    });

    window.addEventListener("resize", () => {
      if (window.innerWidth > 920) closeDrawer();
    });
  }

  // 12. SIDEBAR DASHBOARD NAVIGATION ARCHITECTURE
  function escapeHtml(str) {
    if (!str) return "";
    return String(str)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  const SIDEBAR_NAV_ITEMS = {
    PATIENT: [
      { section: "Main Menu" },
      {
        label: "Dashboard",
        icon: "📊",
        href: "patient-dashboard.html",
        key: "routine",
      },
      {
        label: "Medicines",
        icon: "💊",
        href: "medicines.html",
        key: "medicines",
      },
      {
        label: "Medicine Schedule",
        icon: "⏰",
        href: "schedule.html",
        key: "schedule",
      },
      {
        label: "Medicine History",
        icon: "📋",
        href: "history.html",
        key: "history",
      },
      {
        label: "Notifications",
        icon: "🔔",
        href: "notifications.html",
        key: "alerts",
      },

      { section: "Care & Support" },
      {
        label: "My Caregivers",
        icon: "🩺",
        href: "my-caregivers.html",
        key: "caregivers",
      },
      {
        label: "Health Profile",
        icon: "👤",
        href: "settings.html",
        key: "profile",
      },
      {
        label: "Emergency SOS",
        icon: "🚨",
        href: "emergency.html",
        key: "emergency",
        isSos: true,
      },

      { section: "Tools & Help" },
      {
        label: "AI Health Chat",
        icon: "🤖",
        href: "ai-assistant.html",
        key: "ai",
      },
      {
        label: "Print Dossier",
        icon: "🖨️",
        href: "print-report.html",
        key: "report",
      },
      {
        label: "Help & Guide",
        icon: "❓",
        href: "javascript:void(0)",
        key: "help",
        isHelp: true,
      },
      { label: "Settings", icon: "⚙️", href: "settings.html", key: "settings" },
    ],
    CAREGIVER: [
      { section: "Main Menu" },
      {
        label: "Dashboard",
        icon: "📊",
        href: "caregiver-dashboard.html",
        key: "caregiver",
      },
      {
        label: "My Patients",
        icon: "🩺",
        href: "caregiver-dashboard.html#patientsList",
        key: "mypatients",
      },
      {
        label: "Medicines",
        icon: "💊",
        href: "medicines.html",
        key: "medicines",
      },
      {
        label: "Medicine Schedule",
        icon: "⏰",
        href: "schedule.html",
        key: "schedule",
      },
      {
        label: "Intake History",
        icon: "📋",
        href: "history.html",
        key: "history",
      },
      {
        label: "Notifications",
        icon: "🔔",
        href: "notifications.html",
        key: "alerts",
      },

      { section: "Tools & Help" },
      {
        label: "Health Profile",
        icon: "👤",
        href: "settings.html",
        key: "profile",
      },
      {
        label: "AI Health Chat",
        icon: "🤖",
        href: "ai-assistant.html",
        key: "ai",
      },
      {
        label: "Print Dossier",
        icon: "🖨️",
        href: "print-report.html",
        key: "report",
      },
      {
        label: "Help & Guide",
        icon: "❓",
        href: "javascript:void(0)",
        key: "help",
        isHelp: true,
      },
      { label: "Settings", icon: "⚙️", href: "settings.html", key: "settings" },
    ],
    ADMIN: [
      { section: "Main Menu" },
      {
        label: "Dashboard",
        icon: "📊",
        href: "admin-dashboard.html",
        key: "admin",
      },
      {
        label: "Medicines",
        icon: "💊",
        href: "medicines.html",
        key: "medicines",
      },
      {
        label: "Medicine Schedule",
        icon: "⏰",
        href: "schedule.html",
        key: "schedule",
      },
      {
        label: "Audit History",
        icon: "📋",
        href: "history.html",
        key: "history",
      },
      {
        label: "System Alerts",
        icon: "🔔",
        href: "notifications.html",
        key: "alerts",
      },

      { section: "Tools & Help" },
      {
        label: "AI Health Chat",
        icon: "🤖",
        href: "ai-assistant.html",
        key: "ai",
      },
      {
        label: "Print Dossier",
        icon: "🖨️",
        href: "print-report.html",
        key: "report",
      },
      {
        label: "Help & Guide",
        icon: "❓",
        href: "javascript:void(0)",
        key: "help",
        isHelp: true,
      },
      { label: "Settings", icon: "⚙️", href: "settings.html", key: "settings" },
    ],
  };

  function buildSidebarHTML(meta, session, currentPage) {
    const role = (session.role || "PATIENT").toUpperCase();
    const items = SIDEBAR_NAV_ITEMS[role] || SIDEBAR_NAV_ITEMS.PATIENT;

    const user =
      (window.MediCareAPI && window.MediCareAPI.getCurrentUser()) || {};
    const userName =
      user.name || (user.email ? user.email.split("@")[0] : "User");
    const userInitials = (userName.charAt(0) || "U").toUpperCase();

    let linksHTML = "";
    items.forEach((item) => {
      if (item.section) {
        linksHTML += `<div class="sidebar-section-title">${item.section}</div>`;
        return;
      }

      const isActive =
        item.href === currentPage ||
        (meta.navKey && meta.navKey === item.key) ||
        (item.href === "settings.html" &&
          (currentPage === "settings.html" ||
            currentPage === "profile-settings.html"));

      const activeClass = isActive ? "active" : "";
      const sosClass = item.isSos ? "sos-link" : "";
      const clickHandler = item.isHelp
        ? `onclick="window.openHelpModal(); window.closeSidebar(); return false;"`
        : `onclick="window.closeSidebar()"`;

      linksHTML += `
        <a href="${item.href}" class="sidebar-link ${activeClass} ${sosClass}" ${clickHandler}>
          <span class="sidebar-link-icon">${item.icon}</span>
          <span>${item.label}</span>
        </a>
      `;
    });

    const isPatient = role === "PATIENT";

    return `
      <!-- Mobile Topbar (< 768px) -->
      <div class="medicare-mobile-topbar" id="medicareMobileTopbar">
        <a href="${session.dashboardUrl || "patient-dashboard.html"}" class="sidebar-brand" style="text-decoration: none; display: flex; align-items: center; gap: 8px;">
          <div class="sidebar-logo-icon" style="width: 32px; height: 32px; border-radius: 8px;">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"></path>
            </svg>
          </div>
          <span style="font-weight: 800; font-size: 16px; color: var(--text-main);"><span class="brand-medi">Medi</span><span class="brand-care">Care</span></span>
          <span class="sidebar-role-pill role-${role.toLowerCase()}">${role}</span>
        </a>
        <button class="mobile-hamburger-btn" id="sidebarOpenBtn" onclick="window.openSidebar()" aria-label="Open Navigation Menu" title="Navigation Menu">
          <span></span>
          <span></span>
          <span></span>
        </button>
      </div>

      <!-- Backdrop Overlay -->
      <div class="sidebar-backdrop" id="sidebarBackdrop" onclick="window.closeSidebar()"></div>

      <!-- Fixed Left Sidebar -->
      <aside class="medicare-sidebar" id="medicareSidebar" aria-label="MediCare Dashboard Navigation">
        <div class="sidebar-header">
          <a href="${session.dashboardUrl || "patient-dashboard.html"}" class="sidebar-brand" title="MediCare Dashboard">
            <div class="sidebar-logo-icon">
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                <path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"></path>
              </svg>
            </div>
            <div style="display: flex; align-items: center; gap: 8px;">
              <span class="sidebar-brand-name">MediCare</span>
              <span class="sidebar-role-pill role-${role.toLowerCase()}">${role}</span>
            </div>
          </a>
          <button class="sidebar-close-btn" id="sidebarCloseBtn" onclick="window.closeSidebar()" aria-label="Close Navigation Menu">&times;</button>
        </div>

        <nav class="sidebar-nav">
          ${linksHTML}
        </nav>

        <div class="sidebar-footer">
          <div class="sidebar-user-card">
            <div class="sidebar-user-avatar">${userInitials}</div>
            <div class="sidebar-user-meta">
              <div class="sidebar-user-name">${escapeHtml(userName)}</div>
              <div class="sidebar-user-role">${role} Account</div>
            </div>
          </div>
          <button class="sidebar-signout-btn" onclick="window.handleSignOut()">
            🚪 Sign Out
          </button>
        </div>
      </aside>
    `;
  }

  function initSidebarLayout(meta, session, currentPage, headerMount) {
    document.body.classList.add("has-sidebar");

    // Hide old top header
    if (headerMount) {
      headerMount.style.display = "none";
    }

    // Clean up any existing sidebar or backdrop to prevent duplicates
    const oldSb = document.getElementById("medicareSidebar");
    const oldBd = document.getElementById("sidebarBackdrop");
    const oldMb = document.getElementById("medicareMobileTopbar");
    if (oldSb) oldSb.remove();
    if (oldBd) oldBd.remove();
    if (oldMb) oldMb.remove();

    // Inject sidebar markup into document body
    const sidebarFrag = document
      .createRange()
      .createContextualFragment(buildSidebarHTML(meta, session, currentPage));
    document.body.prepend(sidebarFrag);

    // Escape key closes mobile sidebar
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape") closeSidebar();
    });

    window.addEventListener("resize", () => {
      if (window.innerWidth >= 768) closeSidebar();
    });

    // Global Floating AI Chatbot loader
    initGlobalAiChatbot(currentPage, session);
  }

  // 13. INITIALIZATION & DOM INJECTION
  function initNavbar() {
    const currentPage = getCurrentPageName();
    const session = getUserSession();

    // 1. Strict route guard check
    if (!enforceRouteGuard(session, currentPage)) {
      return;
    }

    // 2. Locate or create header mount element
    let headerMount = document.getElementById("global-header");
    if (!headerMount) {
      headerMount = document.querySelector(
        ".site-header, .top-bar, .web-header",
      );
    }
    if (!headerMount) {
      headerMount = document.createElement("header");
      headerMount.id = "global-header";
      headerMount.className = "site-header";
      document.body.prepend(headerMount);
    }

    headerMount.classList.add("site-header");

    if (currentPage === "print-report.html") {
      headerMount.classList.add("no-print");
    }

    // 3. Resolve Page Metadata
    const customSubtitle = headerMount.getAttribute("data-subtitle");
    const customIcon = headerMount.getAttribute("data-icon");
    const customNavKey = headerMount.getAttribute("data-nav");

    const meta = {
      ...(PAGE_MAP[currentPage] || {
        title: "MediCare",
        subtitle: "Healthcare Platform",
        icon: "+",
        navKey: "",
      }),
    };

    if (customSubtitle) meta.subtitle = customSubtitle;
    if (customIcon) meta.icon = customIcon;
    if (customNavKey) meta.navKey = customNavKey;

    // 4. For authenticated dashboard portals, initialize modern Fixed Left Sidebar layout
    if (
      session.isLoggedIn &&
      currentPage !== "login.html" &&
      currentPage !== "index.html" &&
      currentPage !== ""
    ) {
      initSidebarLayout(meta, session, currentPage, headerMount);
      return;
    }

    // 5. Render Public/Auth Header
    headerMount.innerHTML = buildHeaderHTML(meta, session, currentPage);

    // 5. Remove any previous dynamic drawer elements to avoid duplicates
    const oldOverlay = document.getElementById("mobileOverlay");
    const oldDrawer = document.getElementById("mobileDrawer");
    if (oldOverlay) oldOverlay.remove();
    if (oldDrawer) oldDrawer.remove();

    // 6. Render Drawer right after Header
    if (currentPage !== "print-report.html") {
      const drawerFragment = document
        .createRange()
        .createContextualFragment(buildDrawerHTML(meta, session, currentPage));
      headerMount.parentNode.insertBefore(
        drawerFragment,
        headerMount.nextSibling,
      );
    }

    // 7. Bind interactive listeners
    attachDrawerListeners();
    initHeaderScrollListener();

    // 8. Modular Global AI Chatbot Integration (Excluded on login.html & index.html)
    initGlobalAiChatbot(currentPage, session);
  }

  // Global Floating AI Chatbot loader
  function initGlobalAiChatbot(currentPage, session) {
    if (
      PUBLIC_PAGES.includes(currentPage) ||
      currentPage === "login.html" ||
      currentPage === "index.html" ||
      currentPage === ""
    ) {
      return;
    }

    // Inject CSS if missing
    if (!document.querySelector('link[href*="ai-chatbot.css"]')) {
      const link = document.createElement("link");
      link.rel = "stylesheet";
      link.href = "css/ai-chatbot.css";
      document.head.appendChild(link);
    }

    // Inject JS if missing
    if (!document.querySelector('script[src*="ai-chatbot.js"]')) {
      const script = document.createElement("script");
      script.src = "js/ai-chatbot.js";
      script.defer = true;
      document.body.appendChild(script);
    }
  }

  // Header scroll elevation controller
  function initHeaderScrollListener() {
    const handleScroll = () => {
      const header =
        document.getElementById("global-header") ||
        document.querySelector(".site-header");
      if (!header) return;
      if (window.scrollY > 8) {
        header.classList.add("scrolled");
      } else {
        header.classList.remove("scrolled");
      }
    };
    window.addEventListener("scroll", handleScroll, { passive: true });
    handleScroll();
  }

  // 12. ACCESSIBLE HELP MODAL & TOAST NOTIFICATION HELPERS
  function openHelpModal() {
    let modalBackdrop = document.getElementById("medicareHelpModal");
    if (!modalBackdrop) {
      modalBackdrop = document.createElement("div");
      modalBackdrop.id = "medicareHelpModal";
      modalBackdrop.className = "modal-backdrop";
      modalBackdrop.setAttribute("role", "dialog");
      modalBackdrop.setAttribute("aria-modal", "true");
      modalBackdrop.setAttribute("aria-labelledby", "helpModalTitle");

      modalBackdrop.innerHTML = `
        <div class="modal-box" style="max-width: 540px;">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; border-bottom: 1.5px solid var(--border); padding-bottom: 12px;">
            <h3 id="helpModalTitle" style="font-size: 19px; font-weight: 800; color: var(--text-main); display: flex; align-items: center; gap: 8px;">
              <span>❓</span> MediCare Help & Simple Guide
            </h3>
            <button class="close-btn" onclick="window.closeHelpModal()" aria-label="Close Help Guide">&times;</button>
          </div>

          <div class="help-modal-content">
            <h4>💊 How to Take Your Medicine</h4>
            <ol class="help-steps-list">
              <li>Look at <strong>Today's Routine</strong> on your dashboard.</li>
              <li>Find the medicine scheduled for the current time.</li>
              <li>Take your prescribed dose with water or food as indicated.</li>
              <li>Tap the green <strong>"Mark as Taken"</strong> button and confirm when asked.</li>
            </ol>

            <h4>⏳ How to Set a Reminder</h4>
            <p>
              Not ready to take your dose yet? Tap <strong>"Remind in 15 Minutes"</strong> to snooze your dose. MediCare will remind you again shortly.
            </p>

            <h4>🚨 How Emergency SOS Works</h4>
            <p>
              If you feel unwell or need urgent help, tap the red <strong>"Emergency SOS"</strong> button. The app will ask you to confirm, then immediately send high-priority alerts to your assigned caregivers.
            </p>

            <div class="emergency-quick-box">
              <strong>Need Immediate Medical Attention?</strong>
              <p style="margin: 0; font-size: 13.5px; color: #7f1d1d;">
                If you have difficulty breathing, chest pain, or an acute emergency, please dial emergency services immediately: 
                <a href="tel:112" style="font-weight: 800; color: #b91c1c; text-decoration: underline;">Call 112 (India)</a> or 
                <a href="tel:911" style="font-weight: 800; color: #b91c1c; text-decoration: underline;">Call 911 (US)</a>.
              </p>
            </div>
          </div>

          <div style="margin-top: 20px; display: flex; justify-content: flex-end;">
            <button class="btn btn-primary" style="min-width: 120px;" onclick="window.closeHelpModal()">I Understand</button>
          </div>
        </div>
      `;

      document.body.appendChild(modalBackdrop);

      // Close on backdrop click
      modalBackdrop.addEventListener("click", (e) => {
        if (e.target === modalBackdrop) closeHelpModal();
      });

      // Close on escape
      document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && modalBackdrop.classList.contains("active")) {
          closeHelpModal();
        }
      });
    }

    modalBackdrop.classList.add("active");
    document.body.style.overflow = "hidden";
  }

  function closeHelpModal() {
    const modalBackdrop = document.getElementById("medicareHelpModal");
    if (modalBackdrop) {
      modalBackdrop.classList.remove("active");
    }
    document.body.style.overflow = "";
  }

  function showMediCareToast(message, type = "success") {
    let container = document.getElementById("medicareToastContainer");
    if (!container) {
      container = document.createElement("div");
      container.id = "medicareToastContainer";
      container.className = "medicare-toast-container";
      document.body.appendChild(container);
    }

    const toast = document.createElement("div");
    toast.className = `medicare-toast ${type}`;
    const icon = type === "success" ? "✅" : type === "error" ? "⚠️" : "ℹ️";

    toast.innerHTML = `
      <div style="display: flex; align-items: center; gap: 10px;">
        <span style="font-size: 18px;">${icon}</span>
        <span>${message}</span>
      </div>
      <button style="background: none; border: none; font-size: 18px; cursor: pointer; color: var(--text-muted); padding: 4px;" onclick="this.parentElement.remove()">&times;</button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = "0";
      toast.style.transform = "translateY(10px)";
      toast.style.transition = "all 0.3s ease";
      setTimeout(() => toast.remove(), 300);
    }, 4000);
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
  window.openHelpModal = openHelpModal;
  window.closeHelpModal = closeHelpModal;
  window.showMediCareToast = showMediCareToast;

  // Attach link click interceptor on public pages
  attachPublicLinkInterceptors();

  // Run on DOM ready
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initNavbar);
  } else {
    initNavbar();
  }

  // Reactive listeners: instantly unlock or hide links when user logs in, registers, or logs out
  window.addEventListener("medicare:auth-change", initNavbar);
  window.addEventListener("storage", (e) => {
    if (
      !e.key ||
      e.key.includes("medicare") ||
      e.key.includes("token") ||
      e.key.includes("Role")
    ) {
      initNavbar();
    }
  });
})(window, document);
