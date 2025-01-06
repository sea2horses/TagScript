export const VERSION = 'INDEV';

const NAVBAR_LINKS = {
    'Get Started': 'index.html',
    'All Tags': '#',
    'All Errors': '#',
    GitHub: '#',
};

const EXAMPLE_SIDEBAR = {
    'Sidebar #1': {
        'Item #1': '#',
        'Item #2': '#',
    },
    'Sidebar #2': {},
    'Sidebar #3': {
        'Item #1': '#',
        'Item #2': '#',
        'Item #3': '#',
    },
};

function branding() {
    return `
    <div class="branding">
        <h1>TagScript Docs</h1>
        <h4>(${VERSION})</h4>
    </div>
    `;
}

function navbar(navbar_links) {
    const links = [];
    for (let key in navbar_links) {
        links.push(`<a href="${navbar_links[key]}">${key}</a>`);
    }
    return `<div class="navbar">${links.join('')}</div>`;
}

export function get_header(navbar_links = NAVBAR_LINKS) {
    return `
        ${branding()}
        ${navbar(navbar_links)}
    `;
}

export function get_sidebar(sidebar_content = EXAMPLE_SIDEBAR) {
    const index = [];
    for (let key in sidebar_content) {
        index.push(`<h2 class="sidebar-header">${key}</h2>`);

        const inner_list = sidebar_content[key];
        if (Object.keys(inner_list).length > 0) {
            const list = [];
            for (let item in inner_list) {
                list.push(`<a href="${inner_list[item]}">${item}</a>`);
            }
            index.push(`<div class="sidebar-list">${list.join('')}</div>`);
        }
    }

    return index.join('');
}
