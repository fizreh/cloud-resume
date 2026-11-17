
// ---------- Visitor counter (shows the last known number while the new one loads) ----------
const API_URL = "https://func-resume-faiza-aubce8apgqdjdagb.swedencentral-01.azurewebsites.net/api/visitor";
const CACHE_KEY = "visitorCount";
const REQUEST_TIMEOUT_MS = 70000; // a sleeping database can take about a minute to wake up
const MAX_ATTEMPTS = 3;
const RETRY_DELAY_MS = 5000;

function readCachedCount() {
    try {
        const value = Number(localStorage.getItem(CACHE_KEY));
        return Number.isInteger(value) && value > 0 ? value : null;
    } catch {
        return null; // storage can be blocked by the browser
    }
}

function writeCachedCount(count) {
    try {
        localStorage.setItem(CACHE_KEY, String(count));
    } catch {
        // Ignore: the counter still works, it just isn't remembered.
    }
}

async function fetchCount() {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

    try {
        const response = await fetch(API_URL, { signal: controller.signal });
        if (!response.ok) {
            throw new Error(`Request failed with status ${response.status}`);
        }
        const data = await response.json();
        if (!Number.isInteger(data.count)) {
            throw new Error("Unexpected response from the API");
        }
        return data.count;
    } finally {
        clearTimeout(timer);
    }
}

async function loadVisitorCount() {
    const counter = document.getElementById("visitor-count");
    const cached = readCachedCount();

    // Show the last known number right away, or a loading text on the first visit.
    counter.textContent = cached ?? "wird geladen …";
    counter.classList.add("is-updating");

    try {
        for (let attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
            try {
                const count = await fetchCount();
                counter.textContent = count;
                writeCachedCount(count);
                return;
            } catch (error) {
                console.error(`Visitor count, attempt ${attempt} failed:`, error);
                if (attempt < MAX_ATTEMPTS) {
                    await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
                }
            }
        }

        // All attempts failed: keep the last known number, or say so on a first visit.
        if (cached === null) {
            counter.textContent = "momentan nicht verfügbar";
        }
    } finally {
        counter.classList.remove("is-updating");
    }
}


// ---------- Light / dark mode ----------
function initTheme() {
    const root = document.documentElement;
    const button = document.getElementById("theme-toggle");

    let saved = null;
    try {
        saved = localStorage.getItem("theme");
    } catch {
        // Storage can be blocked by the browser. The page still works without it.
    }

    const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    root.dataset.theme = saved ?? (prefersDark ? "dark" : "light");

    button.addEventListener("click", () => {
        const next = root.dataset.theme === "dark" ? "light" : "dark";
        root.dataset.theme = next;
        try {
            localStorage.setItem("theme", next);
        } catch {
            // Ignore: the choice just isn't remembered.
        }
    });
}

// ---------- Highlight the menu item of the section being read ----------
function initScrollSpy() {
    const links = document.querySelectorAll(".nav-links a");
    const sections = [...links]
        .map((link) => document.querySelector(link.getAttribute("href")))
        .filter(Boolean);

    const observer = new IntersectionObserver(
        (entries) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting) return;
                links.forEach((link) => {
                    link.classList.toggle("active", link.getAttribute("href") === `#${entry.target.id}`);
                });
            });
        },
        { rootMargin: "-40% 0px -55% 0px" }
    );

    sections.forEach((section) => observer.observe(section));
}

// ---------- Start ----------
document.getElementById("year").textContent = new Date().getFullYear();
initTheme();
initScrollSpy();
loadVisitorCount();