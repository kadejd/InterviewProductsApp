// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let categoryRequest;

async function updateCatalog(url, addHistoryEntry) {
    categoryRequest?.abort();
    categoryRequest = new AbortController();

    try {
            const response = await fetch(url, { signal: categoryRequest.signal });
            if (!response.ok) {
                throw new Error(`Category request failed with status ${response.status}.`);
            }

            const page = new DOMParser().parseFromString(await response.text(), "text/html");
            const catalog = document.querySelector(".catalog-shell");
            const updatedCatalog = page.querySelector(".catalog-shell");
            if (!catalog || !updatedCatalog) {
                throw new Error("The category response did not contain the product catalog.");
            }

            catalog.replaceWith(updatedCatalog);
            document.title = page.title;
            if (addHistoryEntry) {
                window.history.pushState({}, "", url);
            }
        } catch (error) {
        if (error.name === "AbortError") {
            return;
        }

        console.error("Unable to update the product category.", error);
        const alert = document.createElement("div");
        alert.className = "alert alert-warning";
        alert.setAttribute("role", "alert");
        alert.textContent = "The category could not be loaded. Please try again.";
        document.querySelector(".catalog-shell")?.prepend(alert);
    }
}

document.addEventListener("click", (event) => {
    const link = event.target.closest("a.category-card, a.catalog-navigation");
    if (
        !link ||
        event.button !== 0 ||
        link.closest(".page-item.disabled") ||
        link.target ||
        link.hasAttribute("download") ||
        new URL(link.href).origin !== window.location.origin
    ) {
        return;
    }

    event.preventDefault();
    updateCatalog(link.href, true);
});

window.addEventListener("popstate", () => {
    const categoryLink = document.querySelector("a.category-card");
    if (categoryLink && new URL(categoryLink.href).pathname === window.location.pathname) {
        updateCatalog(window.location.href, false);
    }
});
