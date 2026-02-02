// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Simple Bootstrap toast helper
window.showToast = function(message, type){
  try{
    const container = document.getElementById('toastContainer');
    if (!container) return alert(message);
    const toastEl = document.createElement('div');
    const theme = (type==='error'?'danger': type==='success'?'success': type==='warning'?'warning':'secondary');
    const icon = type==='success' ? '✅' : type==='error' ? '⚠️' : type==='warning' ? '⚠️' : 'ℹ️';
    toastEl.className = 'toast align-items-center text-bg-' + theme + ' border-0';
    toastEl.setAttribute('role','alert');
    toastEl.setAttribute('aria-live','assertive');
    toastEl.setAttribute('aria-atomic','true');
    toastEl.innerHTML = '<div class="d-flex"><div class="toast-body"><span class="me-2">'+ icon +'</span><span class="fw-semibold">'+ message +'</span></div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div>';
    container.appendChild(toastEl);
    const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
    toast.show();
    toastEl.addEventListener('hidden.bs.toast', ()=> toastEl.remove());
  }catch(e){
    console.error('Toast error', e);
    alert(message);
  }
};

// Modern Navbar - Auto highlight active link
(function() {
  'use strict';
  
  function initActiveNavLinks() {
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.modern-nav-link');
    
    navLinks.forEach(link => {
      const href = link.getAttribute('href');
      if (!href) return;
      
      // Remove query strings and trailing slashes for comparison
      let linkPath = href.split('?')[0].toLowerCase().replace(/\/$/, '');
      const currentPathClean = currentPath.replace(/\/$/, '');
      
      // Normalize root path
      if (linkPath === '' || linkPath === '/') {
        linkPath = '/';
      }
      
      // Check if current path matches or starts with link path
      // Special handling for root path - only match exact root
      if (linkPath === '/') {
        if (currentPathClean === '/' || currentPathClean === '') {
          link.classList.add('active');
        } else {
          link.classList.remove('active');
        }
      } else if (currentPathClean === linkPath || currentPathClean.startsWith(linkPath + '/')) {
        link.classList.add('active');
      } else {
        link.classList.remove('active');
      }
    });
  }
  
  // Initialize on page load
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initActiveNavLinks);
  } else {
    initActiveNavLinks();
  }
})();

/**
 * Centralized Cart System using localStorage
 */
const Cart = {
  _storageKey: 'lapizzaria_cart',

  get() {
    try {
      const data = localStorage.getItem(this._storageKey);
      return data ? JSON.parse(data) : [];
    } catch (e) {
      console.error('Error reading cart from localStorage', e);
      return [];
    }
  },

  save(items) {
    try {
      localStorage.setItem(this._storageKey, JSON.stringify(items));
      // Trigger a custom event so other components (like the header badge) can update
      window.dispatchEvent(new CustomEvent('cart-updated', { detail: { count: this.count() } }));
      this.renderCartUI();
    } catch (e) {
      console.error('Error saving cart to localStorage', e);
    }
  },

  add(product) {
    // product: { id, name, price, imageUrl, type }
    const items = this.get();
    const existing = items.find(i => i.id === product.id && i.type === product.type);

    if (existing) {
      existing.quantity = (existing.quantity || 1) + 1;
    } else {
      items.push({
        id: product.id,
        name: product.name,
        price: product.price,
        imageUrl: product.imageUrl,
        type: product.type || 'product',
        quantity: 1
      });
    }

    this.save(items);
    
    if (window.showToast) {
      window.showToast(`Đã thêm ${product.name} vào giỏ hàng`, 'success');
    }

    // Auto open cart drawer when adding
    const offcanvas = document.getElementById('cartOffcanvas');
    if (offcanvas) {
        const bsOffcanvas = bootstrap.Offcanvas.getInstance(offcanvas) || new bootstrap.Offcanvas(offcanvas);
        bsOffcanvas.show();
    }
  },

  remove(id, type = 'product') {
    let items = this.get();
    items = items.filter(i => !(i.id === id && i.type === type));
    this.save(items);
  },

  updateQuantity(id, type, quantity) {
    const items = this.get();
    const item = items.find(i => i.id === id && i.type === type);
    if (item) {
      item.quantity = Math.max(1, parseInt(quantity) || 1);
      this.save(items);
    }
  },

  count() {
    const items = this.get();
    return items.reduce((sum, item) => sum + (item.quantity || 1), 0);
  },

  totalPrice() {
      const items = this.get();
      return items.reduce((sum, item) => sum + (item.price * (item.quantity || 1)), 0);
  },

  clear() {
    if (confirm('Bạn có muốn xóa tất cả món trong giỏ hàng?')) {
        this.save([]);
    }
  },

  renderCartUI() {
    const container = document.getElementById('cart-items-container');
    const summary = document.getElementById('cart-summary');
    const totalPriceEl = document.getElementById('cart-total-price');
    if (!container || !summary || !totalPriceEl) return;

    const items = this.get();
    if (items.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5">
                <i class="bi bi-cart-x fs-1 text-brand-gray-light"></i>
                <p class="text-brand-gray mt-2">Giỏ hàng trống</p>
            </div>`;
        summary.style.display = 'none';
        return;
    }

    summary.style.display = 'block';
    totalPriceEl.textContent = this.totalPrice().toLocaleString() + '₫';

    container.innerHTML = items.map(item => `
        <div class="d-flex gap-3 mb-3 pb-3 border-bottom align-items-center">
            <div class="rounded-pill overflow-hidden bg-brand-bg flex-shrink-0" style="width: 60px; height: 60px;">
                <img src="${item.imageUrl || '/images/placeholder.png'}" class="w-100 h-100 object-fit-cover" alt="${item.name}">
            </div>
            <div class="flex-grow-1 min-w-0">
                <h6 class="mb-0 fw-bold text-truncate">${item.name}</h6>
                <div class="text-brand-orange fw-black small">${item.price.toLocaleString()}₫</div>
                <div class="d-flex align-items-center gap-2 mt-2">
                    <button class="btn btn-sm btn-light p-0 rounded-circle" style="width: 24px; height: 24px;" onclick="cart.updateQuantity(${item.id}, '${item.type}', ${item.quantity - 1})">-</button>
                    <span class="small fw-bold">${item.quantity}</span>
                    <button class="btn btn-sm btn-light p-0 rounded-circle" style="width: 24px; height: 24px;" onclick="cart.updateQuantity(${item.id}, '${item.type}', ${item.quantity + 1})">+</button>
                </div>
            </div>
            <button class="btn btn-sm text-brand-red p-0" onclick="cart.remove(${item.id}, '${item.type}')">
                <i class="bi bi-trash"></i>
            </button>
        </div>
    `).join('');
  }
};

// Initialize header badge on load
document.addEventListener('DOMContentLoaded', () => {
  const updateBadge = () => {
    const badge = document.getElementById('cart-count');
    if (badge) {
      const count = Cart.count();
      badge.textContent = count;
      badge.style.display = count > 0 ? 'inline-block' : 'none';
    }
  };

  window.addEventListener('cart-updated', updateBadge);
  updateBadge(); // Initial update
  Cart.renderCartUI();
});

window.cart = Cart;

/**
 * Pizza Slider Logic (Smooth Continuous Loop)
 */
const PizzaSlider = {
    content: null,
    container: null,
    items: [],
    itemWidth: 0,
    currentOffset: 0,
    isHovered: false,
    speed: 1, // Pixels per frame
    rafId: null,
    isManualMoving: false,
    autoScrollEnabled: true,

    init() {
        this.content = document.getElementById('pizza-content');
        this.container = document.getElementById('pizza-slider');
        if (!this.content || !this.container) return;

        this.items = Array.from(this.content.children);
        if (this.items.length === 0) return;

        // Calculate width of one item including gap
        const style = window.getComputedStyle(this.content);
        const gap = parseInt(style.gap) || 0;
        this.itemWidth = this.items[0].offsetWidth + gap;

        // Pause on hover
        this.content.addEventListener('mouseenter', () => this.isHovered = true);
        this.content.addEventListener('mouseleave', () => this.isHovered = false);

        // Start animation loop
        this.animate();
    },

    animate() {
        if (this.autoScrollEnabled && !this.isHovered && !this.isManualMoving) {
            this.currentOffset -= this.speed;
        }
        
        this.updatePosition(false);
        this.rafId = requestAnimationFrame(() => this.animate());
    },

    next() {
        this.manualMove(-this.itemWidth);
    },

    prev() {
        this.manualMove(this.itemWidth);
    },

    manualMove(delta) {
        this.autoScrollEnabled = false; // Disable auto-scroll permanently on manual interaction
        if (this.isManualMoving) return;
        this.isManualMoving = true;
        
        this.currentOffset += delta;
        this.content.style.transition = 'transform 0.6s cubic-bezier(0.23, 1, 0.32, 1)';
        this.updatePosition(true);

        setTimeout(() => {
            this.isManualMoving = false;
            this.content.style.transition = 'none';
        }, 600);
    },

    updatePosition(animate = false) {
        if (!this.content) return;

        const totalWidth = (this.items.length / 2) * this.itemWidth;

        // Seamless wrap around
        if (this.currentOffset <= -totalWidth) {
            this.currentOffset += totalWidth;
        } else if (this.currentOffset > 0) {
            this.currentOffset -= totalWidth;
        }

        this.content.style.transform = `translateX(${this.currentOffset}px)`;
    }
};

document.addEventListener('DOMContentLoaded', () => {
    PizzaSlider.init();
    window.pizzaSlider = PizzaSlider;
});
