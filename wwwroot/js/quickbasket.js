(() => {
    const requestToken = document.querySelector('meta[name="request-verification-token"]')?.content ?? "";
    const cartDrawer = document.getElementById("cart-drawer");
    const cartItems = document.getElementById("cart-items");
    const cartToggle = document.getElementById("cart-toggle");
    const countBadge = document.getElementById("cart-count");
    const subtotalLabel = document.getElementById("cart-subtotal");
    const checkoutLink = document.getElementById("cart-checkout");
    const locationDialog = document.getElementById("location-dialog");
    let currentCart = { items: [], itemCount: 0, subtotal: 0 };

    const formatPrice = amount => new Intl.NumberFormat("en-IN", {
        style: "currency",
        currency: "INR",
        maximumFractionDigits: 2
    }).format(amount);

    const showMessage = message => {
        if (!cartItems) return;
        const notice = document.createElement("p");
        notice.className = "qb-cart-message";
        notice.setAttribute("role", "status");
        notice.textContent = message;
        cartItems.prepend(notice);
        window.setTimeout(() => notice.remove(), 3500);
    };

    const post = async (url, payload) => {
        const response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": requestToken
            },
            body: JSON.stringify(payload)
        });
        const result = await response.json();
        if (!response.ok) throw new Error(result.message || "Could not update your cart.");
        currentCart = result;
        renderCart();
    };

    const appendText = (parent, tag, className, value) => {
        const element = document.createElement(tag);
        if (className) element.className = className;
        element.textContent = String(value);
        parent.append(element);
        return element;
    };

    const closeCart = () => {
        if (!cartDrawer) return;
        cartDrawer.classList.remove("is-open");
        cartDrawer.setAttribute("aria-hidden", "true");
        cartToggle?.setAttribute("aria-expanded", "false");
        document.body.classList.remove("qb-cart-open");
    };

    const openCart = () => {
        if (!cartDrawer) return;
        cartDrawer.classList.add("is-open");
        cartDrawer.setAttribute("aria-hidden", "false");
        cartToggle?.setAttribute("aria-expanded", "true");
        document.body.classList.add("qb-cart-open");
    };

    const renderCart = () => {
        if (countBadge) {
            countBadge.textContent = String(currentCart.itemCount);
            countBadge.hidden = currentCart.itemCount < 1;
        }
        if (subtotalLabel) subtotalLabel.textContent = formatPrice(currentCart.subtotal);
        if (checkoutLink) {
            const empty = currentCart.itemCount < 1;
            checkoutLink.href = empty ? "#" : "/Cart/Checkout";
            checkoutLink.tabIndex = empty ? -1 : 0;
            checkoutLink.setAttribute("aria-disabled", empty ? "true" : "false");
        }

        document.querySelectorAll("[data-cart-controls]").forEach(controls => {
            const productId = Number(controls.querySelector("[data-cart-add]")?.dataset.productId ?? controls.closest("[data-product-id]")?.dataset.productId);
            const line = currentCart.items.find(item => item.productId === productId);
            const addButton = controls.querySelector("[data-cart-add]");
            const quantityControl = controls.querySelector("[data-card-quantity]");
            if (addButton) addButton.hidden = Boolean(line);
            if (quantityControl) quantityControl.hidden = !line;
            if (line && quantityControl) {
                const value = quantityControl.querySelector("[data-card-quantity-value]");
                if (value) value.textContent = String(line.quantity);
                const increase = quantityControl.querySelector("[data-card-increase]");
                if (increase) increase.disabled = line.quantity >= line.stockQuantity;
            }
        });

        if (!cartItems) return;
        cartItems.replaceChildren();
        if (currentCart.items.length === 0) {
            appendText(cartItems, "div", "qb-cart-empty", "Your cart is empty. Add a few fresh picks to get started.");
            return;
        }

        currentCart.items.forEach(item => {
            const row = document.createElement("article");
            row.className = "qb-cart-line";
            const image = document.createElement("div");
            image.className = "qb-cart-line-image";
            if (item.imageUrl?.startsWith("/")) {
                const img = document.createElement("img");
                img.src = item.imageUrl;
                img.alt = "";
                img.addEventListener("error", () => img.remove(), { once: true });
                image.append(img);
            } else {
                appendText(image, "span", "", "🥬");
            }
            row.append(image);

            const details = document.createElement("div");
            details.className = "qb-cart-line-copy";
            appendText(details, "strong", "", item.name);
            appendText(details, "small", "", `${item.unit} · ${formatPrice(item.price)}`);
            const controls = document.createElement("div");
            controls.className = "qb-quantity-control";
            const decrease = appendText(controls, "button", "", "−");
            decrease.type = "button";
            decrease.dataset.cartDecrement = String(item.productId);
            decrease.setAttribute("aria-label", `Decrease ${item.name} quantity`);
            appendText(controls, "span", "", item.quantity);
            const increase = appendText(controls, "button", "", "+");
            increase.type = "button";
            increase.dataset.cartIncrement = String(item.productId);
            increase.disabled = item.quantity >= item.stockQuantity;
            increase.setAttribute("aria-label", `Increase ${item.name} quantity`);
            details.append(controls);
            const remove = appendText(details, "button", "qb-cart-remove", "Remove");
            remove.type = "button";
            remove.dataset.cartRemove = String(item.productId);
            remove.setAttribute("aria-label", `Remove ${item.name} from cart`);
            row.append(details);
            appendText(row, "strong", "qb-cart-line-total", formatPrice(item.lineTotal));
            cartItems.append(row);
        });
    };

    const refreshCart = async () => {
        try {
            const response = await fetch("/Cart/Summary", { credentials: "same-origin" });
            if (!response.ok) throw new Error("Could not load your cart.");
            currentCart = await response.json();
            renderCart();
        } catch {
            showMessage("Cart is temporarily unavailable. Please refresh the page.");
        }
    };

    document.addEventListener("click", async event => {
        const target = event.target;
        if (!(target instanceof Element)) return;

        const addButton = target.closest("[data-cart-add]");
        const cardIncrease = target.closest("[data-card-increase]");
        const cardDecrease = target.closest("[data-card-decrease]");
        const cartIncrease = target.closest("[data-cart-increment]");
        const cartDecrease = target.closest("[data-cart-decrement]");
        const cartRemove = target.closest("[data-cart-remove]");

        try {
            if (addButton) {
                await post("/Cart/Add", { productId: Number(addButton.dataset.productId), quantity: 1 });
                if (cartDrawer) openCart();
            } else if (cardIncrease || cartIncrease) {
                const productId = Number((cardIncrease || cartIncrease).dataset.cardIncrease ?? (cardIncrease || cartIncrease).dataset.cartIncrement);
                await post("/Cart/Add", { productId, quantity: 1 });
            } else if (cardDecrease || cartDecrease) {
                const productId = Number((cardDecrease || cartDecrease).dataset.cardDecrease ?? (cardDecrease || cartDecrease).dataset.cartDecrement);
                const line = currentCart.items.find(item => item.productId === productId);
                if (line?.quantity <= 1) await post("/Cart/Remove", { productId, quantity: 1 });
                else if (line) await post("/Cart/SetQuantity", { productId, quantity: line.quantity - 1 });
            } else if (cartRemove) {
                await post("/Cart/Remove", { productId: Number(cartRemove.dataset.cartRemove), quantity: 1 });
            } else if (target.closest("[data-cart-close]")) {
                closeCart();
            }
        } catch (error) {
            showMessage(error instanceof Error ? error.message : "Could not update your cart.");
        }
    });

    cartToggle?.addEventListener("click", openCart);
    document.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            closeCart();
            locationDialog?.close();
        }
    });

    const locationButton = document.getElementById("change-location");
    const locationLabel = document.getElementById("delivery-location");
    const locationSelect = document.getElementById("location-select");
    const savedLocation = localStorage.getItem("quickbasket-location");
    if (savedLocation && locationLabel) locationLabel.textContent = savedLocation;
    if (savedLocation && locationSelect) locationSelect.value = savedLocation;
    locationButton?.addEventListener("click", () => locationDialog?.showModal());
    document.getElementById("save-location")?.addEventListener("click", event => {
        event.preventDefault();
        if (!locationSelect || !locationLabel) return;
        locationLabel.textContent = locationSelect.value;
        localStorage.setItem("quickbasket-location", locationSelect.value);
        locationDialog?.close();
    });

    document.querySelectorAll("[data-product-image]").forEach(image => {
        image.addEventListener("error", () => {
            image.hidden = true;
            image.parentElement?.classList.add("image-failed");
        }, { once: true });
    });

    refreshCart();
})();
