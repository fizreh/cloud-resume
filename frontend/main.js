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

loadVisitorCount();