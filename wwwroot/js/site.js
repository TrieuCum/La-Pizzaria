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
}