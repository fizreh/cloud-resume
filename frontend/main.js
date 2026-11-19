// ---------- Addresses (the base URL comes from config.js) ----------
const API_BASE_URL = window.APP_CONFIG.apiBaseUrl;
const VISITOR_URL = `${API_BASE_URL}/api/visitor`;
const ANALYZE_URL = `${API_BASE_URL}/api/analyze`;

// ---------- Visitor counter (shows the last known number while the new one loads) ----------
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
        const response = await fetch(VISITOR_URL, { signal: controller.signal });
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

        if (cached === null) {
            counter.textContent = "momentan nicht verfügbar";
        }
    } finally {
        counter.classList.remove("is-updating");
    }
}

// ---------- Job-Match ----------
const ANALYZE_TIMEOUT_MS = 30000;
const MIN_JOB_LENGTH = 80;

function renderChips(listElement, items, emptyText) {
    listElement.replaceChildren();
    const values = items.length > 0 ? items : [emptyText];
    values.forEach((value) => {
        const item = document.createElement("li");
        item.textContent = value; // textContent: nothing from the API can run as HTML
        listElement.appendChild(item);
    });
}

async function requestAnalysis(jobDescription) {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), ANALYZE_TIMEOUT_MS);

    try {
        const response = await fetch(ANALYZE_URL, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ jobDescription }),
            signal: controller.signal
        });

        const data = await response.json().catch(() => ({}));
        if (!response.ok) {
            throw new Error(typeof data.error === "string" ? data.error : `Fehler ${response.status}`);
        }
        return data;
    } finally {
        clearTimeout(timer);
    }
}

function showResult(data) {
    const score = Math.min(100, Math.max(0, Math.round(Number(data.score) || 0)));
    const grade = /^[A-E]$/.test(data.grade) ? data.grade : "?";
    const matched = Array.isArray(data.matched) ? data.matched.map(String) : [];
    const missing = Array.isArray(data.missing) ? data.missing.map(String) : [];

    document.getElementById("job-score").textContent = score;
    document.getElementById("job-ring").style.setProperty("--pct", score);
    document.getElementById("job-grade").textContent = grade;
    document.getElementById("job-source").textContent =
        `ATS-ähnliche Schätzung · Keyword-Erkennung: ${String(data.source ?? "unbekannt")}`;

    renderChips(document.getElementById("job-matched"), matched, "Keine Übereinstimmung gefunden");
    renderChips(document.getElementById("job-missing"), missing, "Alles gefunden");
    document.getElementById("job-result").hidden = false;
}

function initJobMatch() {
    const input = document.getElementById("job-text");
    const button = document.getElementById("job-analyze");
    const status = document.getElementById("job-status");
    const counter = document.getElementById("job-counter");
    const result = document.getElementById("job-result");
    if (!input || !button) return;

    const updateCounter = () => {
        counter.textContent = `${input.value.length} / ${input.maxLength} Zeichen`;
    };
    input.addEventListener("input", updateCounter);
    updateCounter();

    button.addEventListener("click", async () => {
        const text = input.value.trim();
        if (text.length < MIN_JOB_LENGTH) {
            status.textContent = `Bitte mindestens ${MIN_JOB_LENGTH} Zeichen einfügen.`;
            return;
        }

        button.disabled = true;
        result.hidden = true;
        status.textContent = "Analyse läuft …";

        try {
            showResult(await requestAnalysis(text));
            status.textContent = "";
        } catch (error) {
            console.error("Job match failed:", error);
            if (error.name === "AbortError") {
                status.textContent = "Zeitüberschreitung. Bitte erneut versuchen.";
            } else if (error instanceof TypeError) {
                status.textContent = "Keine Verbindung zum Server.";
            } else {
                status.textContent = error.message || "Die Analyse ist momentan nicht verfügbar.";
            }
        } finally {
            button.disabled = false;
        }
    });
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
initJobMatch();
loadVisitorCount();