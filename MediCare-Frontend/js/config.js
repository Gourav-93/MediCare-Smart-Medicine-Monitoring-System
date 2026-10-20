// MediCare Configuration
const CONFIG = {
    // Development ASP.NET Core Web API Base URL
    API_BASE: window.MEDICARE_API_BASE || 'http://localhost:5095/api',
    
    // Local storage & session storage keys
    STORAGE_KEYS: {
        TOKEN: 'medicare_jwt_token',
        USER: 'medicare_user_profile',
        THEME: 'medicare_theme'
    },

    // Session duration reminder
    TOKEN_EXPIRY_MINUTES: 60,

    // App Branding
    APP_NAME: 'MediCare',
    APP_TAGLINE: 'Smart Medicine Reminder & Family Monitoring System'
};

// Freeze config to prevent accidental mutations
Object.freeze(CONFIG);
Object.freeze(CONFIG.STORAGE_KEYS);
