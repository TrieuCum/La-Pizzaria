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