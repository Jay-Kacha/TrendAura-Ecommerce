// trendaura Frontend JavaScript

document.addEventListener('DOMContentLoaded', function () {
    // Initialize cart and wishlist badges
    updateCartBadge();
    updateWishlistBadge();

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });

    // Add to cart animation feedback
    const addToCartButtons = document.querySelectorAll('button[type="submit"]');
    addToCartButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const icon = this.querySelector('i.fa-cart-plus');
            if (icon) {
                icon.classList.add('fa-bounce');
                setTimeout(() => {
                    icon.classList.remove('fa-bounce');
                    updateCartBadge(); // Update badge after adding
                }, 500);
            }
        });
    });

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });

    // Star rating widget behavior
    document.querySelectorAll('.star-rating-input').forEach(starWrap => {
        const labels = Array.from(starWrap.querySelectorAll('label'));
        const radios = Array.from(starWrap.querySelectorAll('input[type="radio"]'));

        function clearHover() {
            labels.forEach(l => l.classList.remove('hover'));
        }

        function setSelected() {
            labels.forEach(l => l.classList.remove('selected'));
            const checked = radios.find(r => r.checked);
            if (checked) {
                const forId = checked.id;
                const idx = labels.findIndex(l => l.htmlFor === forId);
                if (idx >= 0) {
                    for (let i = idx; i < labels.length; i++) {
                        labels[i].classList.add('selected');
                    }
                }
            }
        }

        labels.forEach((label, index) => {
            label.addEventListener('mouseenter', () => {
                clearHover();
                for (let i = index; i < labels.length; i++) {
                    labels[i].classList.add('hover');
                }
            });

            label.addEventListener('mouseleave', () => {
                clearHover();
                setSelected();
            });

            label.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                const radio = starWrap.querySelector(`#${label.htmlFor}`);
                if (radio) {
                    radio.checked = true;
                    radio.dispatchEvent(new Event('change', { bubbles: true }));
                    setSelected();
                }
            });
        });

        starWrap.addEventListener('mouseleave', () => {
            clearHover();
            setSelected();
        });

        // initialize selected state from checked radio (for edit or retained values)
        setSelected();
    });
});

/**
 * Update Cart Badge Count
 * Retrieves cart count from session/cookie and updates the badge
 */
function updateCartBadge() {
    try {
        const cartBadge = document.querySelector('.cart-badge');
        const cartCount = document.querySelector('.cart-count');
        if (cartBadge && cartCount) {
            // Get cart count from localStorage or session
            const count = parseInt(localStorage.getItem('cartCount') || sessionStorage.getItem('cartCount') || '0');
            if (count > 0) {
                cartCount.textContent = count;
                cartBadge.style.display = 'flex';
                // Animate cart button
                const cartBtn = document.querySelector('.cart-btn');
                if (cartBtn) {
                    cartBtn.classList.add('updated');
                    setTimeout(() => cartBtn.classList.remove('updated'), 400);
                }
            } else {
                cartBadge.style.display = 'none';
            }
        }
    } catch (error) {
        console.log('Cart badge update: ', error);
    }
}

/**
 * Update Wishlist Badge Count
 * Retrieves wishlist count from session/cookie and updates the badge
 */
function updateWishlistBadge() {
    try {
        const wishlistBadge = document.querySelector('.wishlist-badge');
        const wishlistCount = document.querySelector('.wishlist-count');
        if (wishlistBadge && wishlistCount) {
            // Get wishlist count from localStorage or session
            const count = parseInt(localStorage.getItem('wishlistCount') || sessionStorage.getItem('wishlistCount') || '0');
            if (count > 0) {
                wishlistCount.textContent = count;
                wishlistBadge.style.display = 'flex';
            } else {
                wishlistBadge.style.display = 'none';
            }
        }
    } catch (error) {
        console.log('Wishlist badge update: ', error);
    }
}

function setCartCount(count) {
    localStorage.setItem('cartCount', count);
    sessionStorage.setItem('cartCount', count);
    updateCartBadge();
}

function setWishlistCount(count) {
    localStorage.setItem('wishlistCount', count);
    sessionStorage.setItem('wishlistCount', count);
    updateWishlistBadge();
}

function incrementCartCount() {
    const current = parseInt(localStorage.getItem('cartCount') || '0');
    setCartCount(current + 1);
}

function incrementWishlistCount() {
    const current = parseInt(localStorage.getItem('wishlistCount') || '0');
    setWishlistCount(current + 1);
}

// FIX: Cart Quantity Update Function
function updateQty(btn, change) {
    var container = btn.closest('.input-group');
    if (!container) return;

    var input = container.querySelector('input[name="qty"]');
    if (!input) return;

    var currentValue = parseInt(input.value);
    var min = parseInt(input.getAttribute('min')) || 1;
    var max = parseInt(input.getAttribute('max'));

    var newValue = currentValue + change;

    if (newValue >= min && (isNaN(max) || newValue <= max)) {
        input.value = newValue;
        input.form.submit();
    }
}