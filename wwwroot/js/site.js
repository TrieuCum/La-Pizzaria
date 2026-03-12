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

// Image URL Helper
window.sanitizeImageUrl = function(url) {
    if (!url || typeof url !== 'string') return '/images/v17_1114.png';
    url = url.trim();
    if (url === '' || url === 'null') return '/images/v17_1114.png';
    
    if (url.startsWith('file://')) {
        const parts = url.split('/');
        return '/images/' + parts[parts.length-1];
    }
    if (url.startsWith('http') || url.startsWith('/')) return url;
    return '/images/' + url;
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
  _voucherStorageKey: 'lapizzaria_vouchers',
  _allVouchers: [],

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
      this.updateSummary();
    } catch (e) {
      console.error('Error saving cart to localStorage', e);
    }
  },

  getVoucherIds() {
    try {
        const data = localStorage.getItem(this._voucherStorageKey);
        return data ? JSON.parse(data) : [];
    } catch (e) { return []; }
  },

  saveVoucherIds(ids) {
    localStorage.setItem(this._voucherStorageKey, JSON.stringify(ids));
    this.updateSummary();
  },

  add(product) {
    // product: { id, name, price, imageUrl, type }
    const items = this.get();
    const existing = items.find(i => i.id === product.id && i.type === product.type);
    const sanitizedUrl = window.sanitizeImageUrl(product.imageUrl);

    if (existing) {
      existing.quantity = (existing.quantity || 1) + 1;
    } else {
      items.push({
        id: product.id,
        name: product.name,
        price: product.price,
        imageUrl: sanitizedUrl,
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

  renderCartUI(summaryData = null) {
    const container = document.getElementById('cart-items-container');
    const summary = document.getElementById('cart-summary');
    const subtotalEl = document.getElementById('cart-subtotal');
    const discountEl = document.getElementById('cart-discount');
    const totalPriceEl = document.getElementById('cart-total-price');
    const voucherBoxes = document.querySelectorAll('#globalAppliedVouchers, #globalAppliedVouchersPlaceholder');

    if (!container || !summary || !totalPriceEl) return;

    const items = this.get();
    if (items.length === 0) {
        container.innerHTML = `<div class="text-center py-5"><i class="bi bi-cart-x fs-1 text-brand-gray-light"></i><p class="text-brand-gray mt-2">Giỏ hàng trống</p></div>`;
        summary.style.display = 'none';
        return;
    }

    summary.style.display = 'block';
    
    // Items list
    container.innerHTML = items.map(item => `
        <div class="d-flex gap-3 mb-3 pb-3 border-bottom align-items-center">
            <div class="rounded-pill overflow-hidden bg-brand-bg flex-shrink-0" style="width: 60px; height: 60px;">
                <img src="${window.sanitizeImageUrl(item.imageUrl)}" class="w-100 h-100 object-fit-cover" alt="${item.name}">
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

    // Summary logic
    const subtotal = summaryData?.subtotal || this.totalPrice();
    const discount = summaryData?.voucherDiscount || 0;
    const total = summaryData?.total || subtotal - discount;

    if (subtotalEl) subtotalEl.textContent = subtotal.toLocaleString() + '₫';
    if (discountEl) discountEl.textContent = '- ' + discount.toLocaleString() + '₫';
    totalPriceEl.textContent = total.toLocaleString() + '₫';

    // Applied Vouchers
    if (voucherBoxes.length > 0) {
        voucherBoxes.forEach(box => {
            box.innerHTML = '';
            const vIds = this.getVoucherIds();
            vIds.forEach((id, idx) => {
                const v = this._allVouchers.find(x => x.id === id);
                if (!v) return;
                const b = document.createElement('div');
                b.className = 'badge bg-brand-orange-light text-brand-orange p-2 rounded d-flex align-items-center gap-2 border border-brand-orange';
                b.innerHTML = `<span class="extra-small fw-bold">${v.code}</span><i class="bi bi-x cursor-pointer" onclick="cart.removeVoucher(${idx})"></i>`;
                box.appendChild(b);
            });
        });
    }
  },

  async updateSummary() {
    const items = this.get().map(i => ({ productId: i.id, quantity: i.quantity, unitPrice: i.price }));
    const voucherIds = this.getVoucherIds();
    
    if (items.length === 0) {
        this.renderCartUI();
        return;
    }

    try {
        const res = await fetch('/Order/Preview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ items, voucherIds })
        });
        if (res.ok) {
            const data = await res.json();
            
            // Auto-removal logic: if voucher applied but discount is 0 and subtotal > 0
            if (voucherIds.length > 0 && data.voucherDiscount === 0 && data.subtotal > 0) {
                const currentIds = this.getVoucherIds();
                if (currentIds.length > 0) {
                    this.saveVoucherIds([]); // Clear all vouchers if no longer valid
                    window.showToast('Voucher không còn đủ điều kiện do thay đổi đơn hàng!', 'warning');
                    return; // saveVoucherIds already calls updateSummary again
                }
            }

            this.renderCartUI(data);
        } else if (res.status === 400) {
            // Auto-removal on validation error (e.g. min order value no longer met)
            const currentIds = this.getVoucherIds();
            if (currentIds.length > 0) {
                this.saveVoucherIds([]); // Clear all
                window.showToast('Voucher không còn đủ điều kiện do đơn hàng thay đổi!', 'warning');
            } else {
                this.renderCartUI();
            }
        } else {
            this.renderCartUI();
        }
    } catch (e) {
        console.error('Update summary failed', e);
        this.renderCartUI();
    }
  },

  async loadVouchers() {
    try {
        const res = await fetch('/api/vouchers');
        this._allVouchers = await res.json();
        
        const btn = document.getElementById('openGlobalVoucherModal');
        if (btn) {
            btn.onclick = () => {
                this.updateVoucherModalUI();
                new bootstrap.Modal(document.getElementById('globalVoucherModal')).show();
            };
        }
    } catch (e) { console.error('Load vouchers failed', e); }
  },

  updateVoucherModalUI() {
    const subtotal = this.totalPrice();
    const available = document.getElementById('globalListAvailable');
    const ineligible = document.getElementById('globalListIneligible');
    if (!available || !ineligible) return;

    available.innerHTML = ''; ineligible.innerHTML = '';
    this._allVouchers.forEach(v => {
        const isEligible = subtotal >= (v.minOrderValue || 0);
        
        let discountLabel = '';
        let typeLabel = 'Discount';
        let icon = 'bi-ticket-perforated';

        if(v.type === 'Percentage') {
            discountLabel = `${v.percent}%`;
            typeLabel = 'Giảm giá';
            icon = 'bi-percent';
        } else if(v.type === 'FixedAmount') {
            discountLabel = `${Math.round(v.amount/1000)}k`;
            typeLabel = 'Giảm giá';
            icon = 'bi-cash-stack';
        } else if(v.type === 'FreeShipping') {
            discountLabel = 'FREE';
            typeLabel = 'Vận chuyển';
            icon = 'bi-truck';
        }

        const itemWrap = document.createElement('div');
        itemWrap.innerHTML = `
            <div class="voucher-ticket ${isEligible ? '' : 'ineligible'}">
                <div class="ticket-left">
                    <i class="bi ${icon}"></i>
                    <div class="ticket-percent">${discountLabel}</div>
                    <div class="ticket-type">${typeLabel}</div>
                </div>
                <div class="ticket-right">
                    <div>
                        <div class="ticket-code">${v.code}</div>
                        <div class="v-condition-toggle" onclick="cart.toggleCondition(this)">
                            Xem điều kiện <i class="bi bi-chevron-down extra-small"></i>
                        </div>
                    </div>
                    <div class="d-flex justify-content-end">
                        ${isEligible ? `<button class="ticket-btn-apply" onclick="cart.applyVoucherById(${v.id})">Áp dụng</button>` : `<div class="extra-small text-muted fst-italic">Chưa đủ đ/k</div>`}
                    </div>
                </div>
            </div>
            <div class="voucher-details-expand">
                <div class="fw-bold mb-1">• Chi tiết ưu đãi:</div>
                ${v.type === 'Percentage' ? `Giảm ${v.percent}% tổng đơn hàng.` : (v.type === 'FreeShipping' ? `Miễn phí vận chuyển (tối đa ${(v.amount || 0).toLocaleString()}đ).` : `Giảm ${(v.amount || 0).toLocaleString()}đ cho đơn hàng.`)}
                <div class="mt-1">• Đơn tối thiểu: ${(v.minOrderValue || 0).toLocaleString()}đ</div>
                <div>• Hạn dùng: ${v.expiresAtUtc ? new Date(v.expiresAtUtc).toLocaleDateString('vi-VN') : 'Không thời hạn'}</div>
            </div>`;
        
        if (isEligible) available.appendChild(itemWrap);
        else ineligible.appendChild(itemWrap);
    });
  },

  toggleCondition(el) {
    const ticket = el.closest('.voucher-ticket');
    const panel = ticket.nextElementSibling;
    const isShowing = panel.style.display === 'block';
    
    // Rotate icon
    const icon = el.querySelector('.bi-chevron-down') || el.querySelector('.bi-chevron-up');
    if(icon) {
        icon.className = isShowing ? 'bi bi-chevron-down extra-small' : 'bi bi-chevron-up extra-small';
    }

    panel.style.display = isShowing ? 'none' : 'block';
  },

  applyVoucherById(id) {
    const ids = this.getVoucherIds();
    if (ids.includes(id)) return;
    if (ids.length >= 1) return window.showToast('Chỉ áp dụng được tối đa 1 mã!', 'warning');
    
    ids.push(id);
    this.saveVoucherIds(ids);
    const m = bootstrap.Modal.getInstance(document.getElementById('globalVoucherModal'));
    if (m) m.hide();
  },

  applyManualVoucher() {
    const code = document.getElementById('globalVoucherCode').value.trim().toUpperCase();
    if (!code) return;
    const v = this._allVouchers.find(x => x.code.toUpperCase() === code);
    if (!v) return window.showToast('Mã không tồn tại!', 'error');
    if (this.totalPrice() < (v.minOrderValue || 0)) return window.showToast(`Chưa đủ điều kiện! Đơn hàng cần đạt tối thiểu ${v.minOrderValue.toLocaleString()}đ`, 'warning');
    this.applyVoucherById(v.id);
    document.getElementById('globalVoucherCode').value = '';
  },

  removeVoucher(idx) {
    const ids = this.getVoucherIds();
    ids.splice(idx, 1);
    this.saveVoucherIds(ids);
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
  Cart.loadVouchers();
  Cart.updateSummary();
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

/**
 * Admin Sidebar Toggle Logic
 */
(function() {
    'use strict';
    
    function initSidebar() {
        const toggleBtn = document.getElementById('sidebarToggle');
        const body = document.body;
        const sidebar = document.getElementById('adminSidebar');
        
        if (!sidebar) return;
        
        // Add class to body to indicate sidebar existence
        body.classList.add('has-sidebar');
        
        // Load state from localStorage
        const isCollapsed = localStorage.getItem('sidebar-collapsed') === 'true';
        if (isCollapsed) {
            body.classList.add('sidebar-collapsed');
        }
        
        if (toggleBtn) {
            toggleBtn.addEventListener('click', function() {
                body.classList.toggle('sidebar-collapsed');
                const collapsed = body.classList.contains('sidebar-collapsed');
                localStorage.setItem('sidebar-collapsed', collapsed);
            });
        }
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSidebar);
    } else {
        initSidebar();
    }
})();
