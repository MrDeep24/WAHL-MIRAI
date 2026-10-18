// wwwroot/js/site.js
// Global utility functions and scripts for Wahl Mirai

/**
 * Generates a local data URI with an SVG avatar displaying candidate initials.
 * Avoids transmitting personal names or metadata of minors to external avatar providers.
 * Follows the institutional color palette defined in DESIGN.md.
 *
 * @param {string|null|undefined} name Candidate's full name.
 * @returns {string} SVG data URI (data:image/svg+xml;utf8,...).
 */
function getInitialsAvatarSvgUri(name) {
    var cleaned = (typeof name === 'string' ? name : '')
        .replace(/[\u0000-\u001F\u007F-\u009F]/g, '')
        .trim();

    var codePoints = Array.from(cleaned);
    var initials = '?';
    if (codePoints.length > 0) {
        initials = codePoints.slice(0, 2).join('').toUpperCase();
    }

    var escapedInitials = initials
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&apos;');

    var svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64">' +
        '<circle cx="32" cy="32" r="32" fill="#eff4ff"/>' +
        '<text x="32" y="38" font-family="Inter, sans-serif" font-size="22" font-weight="700" fill="#003466" text-anchor="middle">' +
        escapedInitials +
        '</text></svg>';

    return 'data:image/svg+xml;utf8,' + encodeURIComponent(svg);
}

if (typeof window !== 'undefined') {
    window.getInitialsAvatarSvgUri = getInitialsAvatarSvgUri;
}
