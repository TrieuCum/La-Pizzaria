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

  add(product, size = 'M') {
    // product: { id, name, price, imageUrl, type, id2 }
    const items = this.get();
    const existing = items.find(i => i.id === product.id && i.type === product.type && i.id2 === product.id2 && (i.size || 'M') === size);
    const sanitizedUrl = window.sanitizeImageUrl(product.imageUrl);

    if (existing) {
      existing.quantity = (existing.quantity || 1) + 1;
    } else {
      // Calculate modifier for initial price
      let modifier = 0;
      if (size === 'S') modifier = -30000;
      else if (size === 'L') modifier = 50000;

      items.push({
        id: product.id,
        id2: product.id2 || null,
        name: product.name,
        basePrice: product.basePrice || product.price, // Store the size M price
        price: (product.basePrice || product.price) + modifier,
        imageUrl: sanitizedUrl,
        imageUrl2: product.imageUrl2 ? window.sanitizeImageUrl(product.imageUrl2) : null,
        type: product.type || 'product',
        quantity: 1,
        size: size
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

  addMix(p1, p2, size = 'M') {
    const avgBasePrice = (p1.price + p2.price) / 2;
    const mixName = `Mix: ${p1.name} / ${p2.name}`;
    const mixProduct = {
        id: p1.id,
        id2: p2.id,
        name: mixName,
        basePrice: avgBasePrice,
        price: avgBasePrice, // add() will handle modifier
        imageUrl: '/images/mix-default.png',
        imageUrl2: p2.imageUrl,
        type: 'product'
    };
    this.add(mixProduct, size);
  },

  updateSize(id, type, newSize, id2 = null, oldSize = 'M') {
    const items = this.get();
    // Find item by ID/Type/Size combo
    const item = items.find(i => i.id === id && i.type === type && i.id2 === id2 && (i.size || 'M') === oldSize);
    
    if (item && item.size !== newSize) {
        // Check if an item with the new size already exists - if so, merge them
        const existingNewSize = items.find(i => i.id === id && i.type === type && i.id2 === id2 && (i.size || 'M') === newSize);
        
        if (existingNewSize) {
            existingNewSize.quantity += item.quantity;
            items.splice(items.indexOf(item), 1);
        } else {
            // Update item in place
            item.size = newSize;
            let modifier = 0;
            if (newSize === 'S') modifier = -30000;
            else if (newSize === 'L') modifier = 50000;
            
            item.price = (item.basePrice || item.price) + modifier;
        }
        
        this.save(items);
    }
  },

  remove(id, type = 'product', id2 = null, size = 'M') {
    let items = this.get();
    items = items.filter(i => !(i.id === id && i.type === type && i.id2 === id2 && (i.size || 'M') === size));
    this.save(items);
  },

  updateQuantity(id, type, quantity, id2 = null, size = 'M') {
    const items = this.get();
    const item = items.find(i => i.id === id && i.type === type && i.id2 === id2 && (i.size || 'M') === size);
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
            <div class="rounded-pill overflow-hidden bg-light flex-shrink-0 border" style="width: 60px; height: 60px;">
                <img src="${window.sanitizeImageUrl((item.id2 || (item.name && item.name.startsWith('Mix:'))) ? '/images/mix-default.png' : item.imageUrl)}" class="w-100 h-100 object-fit-cover" alt="${item.name}">
            </div>
            <div class="flex-grow-1 min-w-0">
                <h6 class="mb-0 fw-bold text-truncate">${item.name}</h6>
                <div class="d-flex align-items-center gap-2">
                    <select class="form-select form-select-sm py-0 px-1 border-0 bg-light extra-small fw-bold" 
                            style="width: auto; cursor: pointer;"
                            onchange="cart.updateSize(${item.id}, '${item.type}', this.value, ${item.id2 || 'null'}, '${item.size || 'M'}')">
                        <option value="S" ${item.size === 'S' ? 'selected' : ''}>Size S</option>
                        <option value="M" ${item.size === 'M' || !item.size ? 'selected' : ''}>Size M</option>
                        <option value="L" ${item.size === 'L' ? 'selected' : ''}>Size L</option>
                    </select>
                    <div class="text-brand-orange fw-black small">${item.price.toLocaleString()}₫</div>
                </div>
                <div class="d-flex align-items-center gap-2 mt-2">
                    <button class="btn btn-sm btn-light p-0 rounded-circle" style="width: 24px; height: 24px;" onclick="cart.updateQuantity(${item.id}, '${item.type}', ${item.quantity - 1}, ${item.id2 || 'null'}, '${item.size || 'M'}')">-</button>
                    <span class="small fw-bold">${item.quantity}</span>
                    <button class="btn btn-sm btn-light p-0 rounded-circle" style="width: 24px; height: 24px;" onclick="cart.updateQuantity(${item.id}, '${item.type}', ${item.quantity + 1}, ${item.id2 || 'null'}, '${item.size || 'M'}')">+</button>
                </div>
            </div>
            <button class="btn btn-sm text-brand-red p-0" onclick="cart.remove(${item.id}, '${item.type}', ${item.id2 || 'null'}, '${item.size || 'M'}')">
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
    const items = this.get().map(i => ({ productId: i.id, productId2: i.id2, quantity: i.quantity, unitPrice: i.price, size: i.size || 'M' }));
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

    const formatPercent = (p) => {
      const num = Number(p);
      if (!Number.isFinite(num)) return '';
      const roundedInt = Math.round(num);
      if (Math.abs(num - roundedInt) < 1e-9) return String(roundedInt);
      return String(num).replace(/(\.\d*?[1-9])0+$/, '$1');
    };

    const cartItems = this.get();
    available.innerHTML = ''; ineligible.innerHTML = '';
    
    this._allVouchers.forEach((v, idx) => {
        const hasTargetProduct = !v.targetProductId || cartItems.some(i => i.id === v.targetProductId || i.id2 === v.targetProductId);
        const isEligible = hasTargetProduct && subtotal >= (v.minOrderValue || 0);
        
        let discountLabel = '';
        let typeLabel = 'Giảm giá';
        let icon = 'bi-ticket-perforated';

        if(v.type === 'Percentage') {
            discountLabel = `${formatPercent(v.percent)}%`;
            icon = 'bi-percent';
        } else if(v.type === 'FixedAmount') {
            discountLabel = `${Math.round(v.amount/1000)}k`;
            icon = 'bi-cash-stack';
        } else if(v.type === 'FreeShipping') {
            discountLabel = 'FREE';
            typeLabel = 'Vận chuyển';
            icon = 'bi-truck';
        }

        // Progress calculation
        let progressHtml = '';
        if (v.minOrderValue > 0 && subtotal < v.minOrderValue) {
            const percentage = Math.min(100, Math.round((subtotal / v.minOrderValue) * 100));
            const remaining = v.minOrderValue - subtotal;
            progressHtml = `
                <div class="ticket-progress-container">
                    <div class="ticket-progress-bar">
                        <div class="ticket-progress-fill" style="width: ${percentage}%"></div>
                    </div>
                    <span class="ticket-progress-text">Mua thêm ${remaining.toLocaleString()}đ để dùng mã</span>
                </div>`;
        }

        const itemWrap = document.createElement('div');
        itemWrap.className = 'stagger-item';
        itemWrap.style.animationDelay = `${idx * 0.1}s`;
        itemWrap.innerHTML = `
            <div class="voucher-ticket ${isEligible ? '' : 'ineligible'}">
                <div class="ticket-left">
                    <i class="bi ${icon}"></i>
                    <div class="ticket-percent">${discountLabel}</div>
                    <div class="ticket-type">${typeLabel}</div>
                </div>
                <div class="ticket-right">
                    <div class="ticket-header">
                        <div class="d-flex align-items-center gap-2">
                            <span class="ticket-code">${v.code}</span>
                            <button class="btn-copy-code" onclick="event.stopPropagation(); cart.copyToClipboard('${v.code}')">Sao chép</button>
                        </div>
                        ${isEligible ? `<button class="ticket-btn-apply" onclick="cart.applyVoucherById(${v.id})">Dùng ngay</button>` : `<div class="extra-small text-muted fw-bold">${!hasTargetProduct ? 'Thiếu món' : 'Chưa đủ đ/k'}</div>`}
                    </div>
                    ${progressHtml}
                    <div class="v-condition-toggle" onclick="cart.toggleCondition(this)">
                        Chi tiết điều kiện <i class="bi bi-chevron-right extra-small"></i>
                    </div>
                </div>
            </div>
            <div class="voucher-details-expand">
                <div class="fw-bold mb-2 text-dark">• Ưu đãi:</div>
                <p class="mb-2">${v.type === 'Percentage' ? `Giảm ${formatPercent(v.percent)}% tổng giá trị đơn hàng.` : (v.type === 'FreeShipping' ? `Miễn phí vận chuyển cho đơn hàng.` : `Giảm giá trực tiếp ${(v.amount || 0).toLocaleString()}đ.`)}</p>
                <div class="fw-bold mb-2 text-dark">• Điều kiện áp dụng:</div>
                <ul class="list-unstyled mb-0 px-2 extra-small">
                    <li class="mb-1"><i class="bi bi-check2-circle text-success me-1"></i>Đơn hàng tối thiểu: ${(v.minOrderValue || 0).toLocaleString()}đ</li>
                    ${v.targetProductId ? `<li class="mb-1"><i class="bi bi-check2-circle text-primary me-1"></i>Yêu cầu có món: <span class="fw-bold">${v.targetProductName || 'Sản phẩm chỉ định'}</span></li>` : ''}
                    <li><i class="bi bi-clock text-warning me-1"></i>Hạn dùng: ${v.expiresAt ? new Date(v.expiresAt).toLocaleDateString('vi-VN') : 'Không giới hạn'}</li>
                </ul>
            </div>`;
        
        if (isEligible) available.appendChild(itemWrap);
        else ineligible.appendChild(itemWrap);
    });
  },

  copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        window.showToast('Đã sao chép mã voucher!', 'success');
    }).catch(err => {
        console.error('Failed to copy: ', err);
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
