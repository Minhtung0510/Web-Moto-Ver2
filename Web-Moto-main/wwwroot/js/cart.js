// Micro-interactions
document.addEventListener('DOMContentLoaded', () => {
  // ripple on gradient buttons
  document.querySelectorAll('.btn-gradient').forEach(btn => {
    btn.addEventListener('pointerdown', e => {
      const r = document.createElement('span');
      const rect = btn.getBoundingClientRect();
      r.className = 'ripple';
      const size = Math.max(rect.width, rect.height);
      r.style.width = r.style.height = size + 'px';
      r.style.left = (e.clientX - rect.left - size/2) + 'px';
      r.style.top  = (e.clientY - rect.top  - size/2) + 'px';
      btn.appendChild(r);
      setTimeout(()=> r.remove(), 600);
    });
  });

  // soft remove animation
  document.querySelectorAll('[data-remove]').forEach(link => {
    link.addEventListener('click', (e) => {
      const row = e.target.closest('.cart-row');
      if (!row) return;
      row.style.transition = 'transform .25s ease, opacity .25s ease';
      row.style.transform = 'translateX(8px)';
      row.style.opacity = '0.4';
      // let navigation proceed
    });
  });
});
