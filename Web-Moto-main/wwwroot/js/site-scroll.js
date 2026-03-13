document.addEventListener("DOMContentLoaded", function () {
    const params = new URLSearchParams(window.location.search);
    if (params.has("categoryName") || params.has("categoryId") || 
        params.has("brand") || params.has("sortBy")) {
        const productSection = document.getElementById("products"); // ✅ id="products"
        if (productSection) {
            setTimeout(() => {
                productSection.scrollIntoView({ behavior: "smooth" });
            }, 300);
        }
    }
});