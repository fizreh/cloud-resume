const API_URL = "https://func-resume-faiza-aubce8apgqdjdagb.swedencentral-01.azurewebsites.net/api/visitor";

async function loadVisitorCount() {
    const counter = document.getElementById("visitor-count");
    counter.textContent = "...";

    try {
        const response = await fetch(API_URL);
        if (!response.ok) {
            throw new Error(`Request failed with status ${response.status}`);
        }
        const data = await response.json();
        counter.textContent = data.count;
    } catch (error) {
        console.error("Could not load visitor count:", error);
        counter.textContent = "unavailable";
    }
}

//New Code

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