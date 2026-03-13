// ============================================
// SMOOTH SCROLL & NAVBAR EFFECTS
// ============================================
document.addEventListener('DOMContentLoaded', function() {
  const navbar = document.querySelector('.navbar');
  
  // Navbar scroll effect
  window.addEventListener('scroll', function() {
    if (window.scrollY > 50) {
      navbar.classList.add('scrolled');
    } else {
      navbar.classList.remove('scrolled');
    }
  });
  
  // Smooth scroll for anchor links
  document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function(e) {
      e.preventDefault();
      const target = document.querySelector(this.getAttribute('href'));
      if (target) {
        target.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        });
      }
    });
  });
});

// ============================================
// SCROLL REVEAL ANIMATION
// ============================================
const observerOptions = {
  threshold: 0.1,
  rootMargin: '0px 0px -50px 0px'
};

const observer = new IntersectionObserver(function(entries) {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      entry.target.classList.add('active');
    }
  });
}, observerOptions);

// Observe all elements with scroll-reveal class
document.querySelectorAll('.scroll-reveal').forEach(el => {
  observer.observe(el);
});

// Auto-add scroll-reveal to common elements
document.addEventListener('DOMContentLoaded', function() {
  const elements = document.querySelectorAll('.product-card, .feature-box, .brand-logo, .testimonial-card');
  elements.forEach(el => {
    el.classList.add('scroll-reveal');
    observer.observe(el);
  });
});

// ============================================
// COUNTER ANIMATION FOR STATS
// ============================================
function animateCounter(element, target, duration = 2000) {
  let start = 0;
  const increment = target / (duration / 16);
  const timer = setInterval(() => {
    start += increment;
    if (start >= target) {
      element.textContent = Math.ceil(target);
      clearInterval(timer);
    } else {
      element.textContent = Math.ceil(start);
    }
  }, 16);
}

// Trigger counter animation when stats cards are visible
const statsObserver = new IntersectionObserver(function(entries) {
  entries.forEach(entry => {
    if (entry.isIntersecting && !entry.target.dataset.animated) {
      const h2 = entry.target.querySelector('h2');
      if (h2) {
        const text = h2.textContent;
        const number = parseInt(text.replace(/[^0-9]/g, ''));
        if (!isNaN(number)) {
          h2.textContent = '0';
          animateCounter(h2, number, 1500);
          
          // Restore original format after animation
          setTimeout(() => {
            h2.textContent = text;
          }, 1600);
        }
        entry.target.dataset.animated = 'true';
      }
    }
  });
}, { threshold: 0.5 });

document.querySelectorAll('.card.bg-primary, .card.bg-success, .card.bg-info, .card.bg-warning').forEach(card => {
  statsObserver.observe(card);
});

// ============================================
// BACK TO TOP BUTTON
// ============================================
(function() {
  const backToTop = document.createElement('button');
  backToTop.classList.add('back-to-top');
  backToTop.innerHTML = '<i class="fas fa-arrow-up"></i>';
  backToTop.setAttribute('aria-label', 'Back to top');
  document.body.appendChild(backToTop);
  
  window.addEventListener('scroll', function() {
    if (window.scrollY > 300) {
      backToTop.classList.add('show');
    } else {
      backToTop.classList.remove('show');
    }
  });
  
  backToTop.addEventListener('click', function() {
    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  });
})();

// ============================================
// PRODUCT CARD HOVER EFFECTS
// ============================================
document.querySelectorAll('.product-card').forEach(card => {
  card.addEventListener('mouseenter', function() {
    this.style.zIndex = '10';
  });
  
  card.addEventListener('mouseleave', function() {
    this.style.zIndex = '1';
  });
});

// ============================================
// TOAST NOTIFICATIONS
// ============================================
function showToast(message, type = 'success') {
  const toastContainer = document.querySelector('.toast-container') || createToastContainer();
  
  const toast = document.createElement('div');
  toast.classList.add('toast', 'show', `bg-${type}`, 'text-white');
  toast.setAttribute('role', 'alert');
  toast.innerHTML = `
    <div class="toast-body d-flex justify-content-between align-items-center">
      <span>${message}</span>
      <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
    </div>
  `;
  
  toastContainer.appendChild(toast);
  
  // Auto remove after 3 seconds
  setTimeout(() => {
    toast.classList.remove('show');
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}

function createToastContainer() {
  const container = document.createElement('div');
  container.classList.add('toast-container', 'position-fixed', 'top-0', 'end-0', 'p-3');
  container.style.zIndex = '9999';
  document.body.appendChild(container);
  return container;
}

// Example: Show toast on add to cart
document.addEventListener('click', function(e) {
  if (e.target.closest('.btn-add-cart')) {
    e.preventDefault();
    showToast('Sản phẩm đã được thêm vào giỏ hàng!', 'success');
  }
});

// ============================================
// IMAGE LAZY LOADING
// ============================================
if ('IntersectionObserver' in window) {
  const imageObserver = new IntersectionObserver((entries, observer) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const img = entry.target;
        if (img.dataset.src) {
          img.src = img.dataset.src;
          img.classList.add('loaded');
          observer.unobserve(img);
        }
      }
    });
  });
  
  document.querySelectorAll('img[data-src]').forEach(img => {
    imageObserver.observe(img);
  });
}

// ============================================
// FORM VALIDATION ANIMATIONS
// ============================================
document.querySelectorAll('form').forEach(form => {
  form.addEventListener('submit', function(e) {
    const inputs = this.querySelectorAll('.form-control, .form-select');
    let isValid = true;
    
    inputs.forEach(input => {
      if (!input.checkValidity()) {
        isValid = false;
        input.classList.add('is-invalid');
        input.style.animation = 'shake 0.5s';
        setTimeout(() => {
          input.style.animation = '';
        }, 500);
      } else {
        input.classList.remove('is-invalid');
        input.classList.add('is-valid');
      }
    });
    
    if (!isValid) {
      e.preventDefault();
      showToast('Vui lòng kiểm tra lại thông tin!', 'danger');
    }
  });
});

// Add shake animation to CSS dynamically
const style = document.createElement('style');
style.textContent = `
  @keyframes shake {
    0%, 100% { transform: translateX(0); }
    10%, 30%, 50%, 70%, 90% { transform: translateX(-5px); }
    20%, 40%, 60%, 80% { transform: translateX(5px); }
  }
`;
document.head.appendChild(style);

// ============================================
// PRODUCT COMPARISON
// ============================================
let compareList = JSON.parse(localStorage.getItem('compareList')) || [];

function updateCompareCount() {
  const badge = document.getElementById('compare-count');
  if (badge) {
    badge.textContent = compareList.length;
    badge.style.display = compareList.length > 0 ? 'inline-block' : 'none';
  }
}

document.addEventListener('click', function(e) {
  if (e.target.closest('.btn-compare')) {
    e.preventDefault();
    const btn = e.target.closest('.btn-compare');
    const productId = btn.dataset.productId;
    
    if (compareList.includes(productId)) {
      compareList = compareList.filter(id => id !== productId);
      btn.classList.remove('active');
      showToast('Đã xóa khỏi danh sách so sánh', 'info');
    } else {
      if (compareList.length >= 4) {
        showToast('Chỉ có thể so sánh tối đa 4 sản phẩm!', 'warning');
        return;
      }
      compareList.push(productId);
      btn.classList.add('active');
      showToast('Đã thêm vào danh sách so sánh!', 'success');
    }
    
    localStorage.setItem('compareList', JSON.stringify(compareList));
    updateCompareCount();
  }
});

updateCompareCount();

// ============================================
// SEARCH SUGGESTIONS (Debounced)
// ============================================
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

const searchInput = document.querySelector('.search-bar input');
if (searchInput) {
  const handleSearch = debounce(function(e) {
    const query = e.target.value;
    if (query.length >= 2) {
      console.log('Searching for:', query);
      // Here you can add AJAX call to fetch search suggestions
    }
  }, 300);
  
  searchInput.addEventListener('input', handleSearch);
}

// ============================================
// RATING STARS INTERACTION
// ============================================
document.querySelectorAll('.rating').forEach(rating => {
  const stars = rating.querySelectorAll('i');
  
  stars.forEach((star, index) => {
    star.addEventListener('click', function() {
      stars.forEach((s, i) => {
        if (i <= index) {
          s.classList.add('active', 'fas');
          s.classList.remove('far');
        } else {
          s.classList.remove('active', 'fas');
          s.classList.add('far');
        }
      });
      
      // Store rating value
      rating.dataset.rating = index + 1;
      showToast(`Bạn đã đánh giá ${index + 1} sao!`, 'success');
    });
    
    star.addEventListener('mouseenter', function() {
      stars.forEach((s, i) => {
        if (i <= index) {
          s.classList.add('fas');
          s.classList.remove('far');
        }
      });
    });
  });
  
  rating.addEventListener('mouseleave', function() {
    const currentRating = parseInt(this.dataset.rating || 0);
    stars.forEach((s, i) => {
      if (i < currentRating) {
        s.classList.add('active', 'fas');
        s.classList.remove('far');
      } else {
        s.classList.remove('active', 'fas');
        s.classList.add('far');
      }
    });
  });
});

// ============================================
// IMAGE GALLERY LIGHTBOX
// ============================================
document.querySelectorAll('.gallery-item').forEach(item => {
  item.addEventListener('click', function() {
    const imgSrc = this.querySelector('img').src;
    const lightbox = createLightbox(imgSrc);
    document.body.appendChild(lightbox);
    
    setTimeout(() => {
      lightbox.classList.add('show');
    }, 10);
  });
});

function createLightbox(imgSrc) {
  const lightbox = document.createElement('div');
  lightbox.classList.add('lightbox-overlay');
  lightbox.innerHTML = `
    <div class="lightbox-content">
      <button class="lightbox-close">&times;</button>
      <img src="${imgSrc}" alt="Preview">
    </div>
  `;
  
  // Add styles
  lightbox.style.cssText = `
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0,0,0,0.95);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 10000;
    opacity: 0;
    transition: opacity 0.3s ease;
  `;
  
  const content = lightbox.querySelector('.lightbox-content');
  content.style.cssText = `
    position: relative;
    max-width: 90%;
    max-height: 90%;
    animation: zoomIn 0.3s ease;
  `;
  
  const img = lightbox.querySelector('img');
  img.style.cssText = `
    max-width: 100%;
    max-height: 90vh;
    border-radius: 8px;
    box-shadow: 0 0 50px rgba(255,255,255,0.1);
  `;
  
  const closeBtn = lightbox.querySelector('.lightbox-close');
  closeBtn.style.cssText = `
    position: absolute;
    top: -40px;
    right: 0;
    background: none;
    border: none;
    color: white;
    font-size: 40px;
    cursor: pointer;
    transition: transform 0.3s ease;
  `;
  
  closeBtn.addEventListener('mouseenter', () => {
    closeBtn.style.transform = 'rotate(90deg)';
  });
  
  closeBtn.addEventListener('mouseleave', () => {
    closeBtn.style.transform = 'rotate(0deg)';
  });
  
  // Close lightbox
  const closeLightbox = () => {
    lightbox.classList.remove('show');
    setTimeout(() => lightbox.remove(), 300);
  };
  
  closeBtn.addEventListener('click', closeLightbox);
  lightbox.addEventListener('click', function(e) {
    if (e.target === this) {
      closeLightbox();
    }
  });
  
  // ESC key to close
  document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
      closeLightbox();
    }
  }, { once: true });
  
  return lightbox;
}

// Add zoom animation
const zoomStyle = document.createElement('style');
zoomStyle.textContent = `
  @keyframes zoomIn {
    from {
      transform: scale(0.5);
      opacity: 0;
    }
    to {
      transform: scale(1);
      opacity: 1;
    }
  }
  .lightbox-overlay.show {
    opacity: 1 !important;
  }
`;
document.head.appendChild(zoomStyle);

// ============================================
// QUANTITY CONTROLS
// ============================================
document.addEventListener('click', function(e) {
  if (e.target.closest('.quantity-btn')) {
    const btn = e.target.closest('.quantity-btn');
    const input = btn.parentElement.querySelector('.quantity-input');
    let value = parseInt(input.value) || 1;
    
    if (btn.classList.contains('quantity-plus')) {
      value++;
    } else if (btn.classList.contains('quantity-minus') && value > 1) {
      value--;
    }
    
    input.value = value;
    
    // Add pulse effect
    input.style.animation = 'pulse 0.3s ease';
    setTimeout(() => {
      input.style.animation = '';
    }, 300);
  }
});

// ============================================
// STICKY SIDEBAR (for product details page)
// ============================================
const stickySidebar = document.querySelector('.sticky-sidebar');
if (stickySidebar) {
  const stickyOffset = stickySidebar.offsetTop;
  
  window.addEventListener('scroll', function() {
    if (window.pageYOffset > stickyOffset) {
      stickySidebar.classList.add('is-sticky');
    } else {
      stickySidebar.classList.remove('is-sticky');
    }
  });
}

// ============================================
// TABS ANIMATION
// ============================================
document.querySelectorAll('.nav-tabs .nav-link').forEach(tab => {
  tab.addEventListener('click', function(e) {
    e.preventDefault();
    
    // Remove active from all tabs
    document.querySelectorAll('.nav-tabs .nav-link').forEach(t => {
      t.classList.remove('active');
    });
    
    // Add active to clicked tab
    this.classList.add('active');
    
    // Show corresponding tab content
    const targetId = this.getAttribute('href');
    document.querySelectorAll('.tab-pane').forEach(pane => {
      pane.classList.remove('show', 'active');
    });
    
    const targetPane = document.querySelector(targetId);
    if (targetPane) {
      targetPane.classList.add('show', 'active');
      targetPane.style.animation = 'fadeInUp 0.5s ease';
    }
  });
});

// ============================================
// COPY TO CLIPBOARD
// ============================================
function copyToClipboard(text) {
  if (navigator.clipboard && window.isSecureContext) {
    navigator.clipboard.writeText(text).then(() => {
      showToast('Đã sao chép vào clipboard!', 'success');
    });
  } else {
    // Fallback for older browsers
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    try {
      document.execCommand('copy');
      showToast('Đã sao chép vào clipboard!', 'success');
    } catch (err) {
      console.error('Failed to copy:', err);
    }
    document.body.removeChild(textArea);
  }
}

// Add copy buttons to code blocks
document.querySelectorAll('pre code').forEach(block => {
  const copyBtn = document.createElement('button');
  copyBtn.classList.add('btn', 'btn-sm', 'btn-outline-secondary', 'copy-btn');
  copyBtn.innerHTML = '<i class="fas fa-copy"></i>';
  copyBtn.style.cssText = 'position: absolute; top: 5px; right: 5px;';
  
  const wrapper = document.createElement('div');
  wrapper.style.position = 'relative';
  block.parentNode.style.position = 'relative';
  block.parentNode.appendChild(copyBtn);
  
  copyBtn.addEventListener('click', () => {
    copyToClipboard(block.textContent);
    copyBtn.innerHTML = '<i class="fas fa-check"></i>';
    setTimeout(() => {
      copyBtn.innerHTML = '<i class="fas fa-copy"></i>';
    }, 2000);
  });
});

// ============================================
// PRICE FORMATTER
// ============================================
function formatPrice(price) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND'
  }).format(price);
}

// Auto format prices on page
document.querySelectorAll('[data-price]').forEach(el => {
  const price = parseInt(el.dataset.price);
  el.textContent = formatPrice(price);
});

// ============================================
// COUNTDOWN TIMER (for flash sales)
// ============================================
function startCountdown(endDate, elementId) {
  const countdownEl = document.getElementById(elementId);
  if (!countdownEl) return;
  
  const updateCountdown = () => {
    const now = new Date().getTime();
    const distance = endDate - now;
    
    if (distance < 0) {
      countdownEl.innerHTML = '<span class="text-danger">Đã kết thúc</span>';
      return;
    }
    
    const days = Math.floor(distance / (1000 * 60 * 60 * 24));
    const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((distance % (1000 * 60)) / 1000);
    
    countdownEl.innerHTML = `
      <div class="countdown-timer d-flex gap-2">
        <div class="countdown-item">
          <span class="countdown-value">${days}</span>
          <span class="countdown-label">Ngày</span>
        </div>
        <div class="countdown-separator">:</div>
        <div class="countdown-item">
          <span class="countdown-value">${hours.toString().padStart(2, '0')}</span>
          <span class="countdown-label">Giờ</span>
        </div>
        <div class="countdown-separator">:</div>
        <div class="countdown-item">
          <span class="countdown-value">${minutes.toString().padStart(2, '0')}</span>
          <span class="countdown-label">Phút</span>
        </div>
        <div class="countdown-separator">:</div>
        <div class="countdown-item">
          <span class="countdown-value">${seconds.toString().padStart(2, '0')}</span>
          <span class="countdown-label">Giây</span>
        </div>
      </div>
    `;
  };
  
  updateCountdown();
  setInterval(updateCountdown, 1000);
}

// Example usage:
// startCountdown(new Date('2025-12-31 23:59:59').getTime(), 'flash-sale-timer');

// ============================================
// PRODUCT QUICK VIEW
// ============================================
document.addEventListener('click', function(e) {
  if (e.target.closest('.btn-quick-view')) {
    e.preventDefault();
    const btn = e.target.closest('.btn-quick-view');
    const productId = btn.dataset.productId;
    
    // Create quick view modal
    const modal = createQuickViewModal(productId);
    document.body.appendChild(modal);
    
    // Show modal with Bootstrap
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
    
    // Remove modal after hide
    modal.addEventListener('hidden.bs.modal', function() {
      modal.remove();
    });
  }
});

function createQuickViewModal(productId) {
  const modal = document.createElement('div');
  modal.classList.add('modal', 'fade');
  modal.innerHTML = `
    <div class="modal-dialog modal-lg modal-dialog-centered">
      <div class="modal-content">
        <div class="modal-header border-0">
          <h5 class="modal-title">Xem nhanh sản phẩm</h5>
          <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
        </div>
        <div class="modal-body">
          <div class="row">
            <div class="col-md-6">
              <div class="product-image-preview">
                <div class="skeleton skeleton-image mb-3"></div>
              </div>
            </div>
            <div class="col-md-6">
              <div class="skeleton skeleton-title mb-3"></div>
              <div class="skeleton skeleton-text mb-2"></div>
              <div class="skeleton skeleton-text mb-2"></div>
              <div class="skeleton skeleton-text mb-4"></div>
              <button class="btn btn-primary btn-lg w-100">
                <i class="fas fa-shopping-cart me-2"></i>Thêm vào giỏ
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `;
  
  // Here you would typically fetch product data via AJAX
  // For now, we're just showing a skeleton loader
  
  return modal;
}

// ============================================
// WISHLIST FUNCTIONALITY
// ============================================
let wishlist = JSON.parse(localStorage.getItem('wishlist')) || [];

function updateWishlistUI() {
  document.querySelectorAll('.btn-wishlist').forEach(btn => {
    const productId = btn.dataset.productId;
    if (wishlist.includes(productId)) {
      btn.classList.add('active');
      btn.querySelector('i').classList.remove('far');
      btn.querySelector('i').classList.add('fas');
    }
  });
}

document.addEventListener('click', function(e) {
  if (e.target.closest('.btn-wishlist')) {
    e.preventDefault();
    const btn = e.target.closest('.btn-wishlist');
    const productId = btn.dataset.productId;
    const icon = btn.querySelector('i');
    
    if (wishlist.includes(productId)) {
      wishlist = wishlist.filter(id => id !== productId);
      icon.classList.remove('fas');
      icon.classList.add('far');
      btn.classList.remove('active');
      showToast('Đã xóa khỏi danh sách yêu thích', 'info');
    } else {
      wishlist.push(productId);
      icon.classList.remove('far');
      icon.classList.add('fas');
      btn.classList.add('active');
      showToast('Đã thêm vào danh sách yêu thích!', 'success');
      
      // Heart animation
      btn.style.animation = 'heartBeat 0.6s ease';
      setTimeout(() => {
        btn.style.animation = '';
      }, 600);
    }
    
    localStorage.setItem('wishlist', JSON.stringify(wishlist));
  }
});

updateWishlistUI();

// Add heart beat animation
const heartStyle = document.createElement('style');
heartStyle.textContent = `
  @keyframes heartBeat {
    0%, 100% { transform: scale(1); }
    25% { transform: scale(1.3); }
    50% { transform: scale(1.1); }
    75% { transform: scale(1.25); }
  }
`;
document.head.appendChild(heartStyle);

// ============================================
// FILTER SIDEBAR TOGGLE (Mobile)
// ============================================
const filterToggle = document.querySelector('.filter-toggle');
const filterSidebar = document.querySelector('.filter-sidebar');

if (filterToggle && filterSidebar) {
  filterToggle.addEventListener('click', function() {
    filterSidebar.classList.toggle('show');
    document.body.style.overflow = filterSidebar.classList.contains('show') ? 'hidden' : '';
  });
  
  // Close filter when clicking outside
  document.addEventListener('click', function(e) {
    if (!e.target.closest('.filter-sidebar') && !e.target.closest('.filter-toggle')) {
      filterSidebar.classList.remove('show');
      document.body.style.overflow = '';
    }
  });
}

// ============================================
// AUTO SAVE FORM DATA (Draft)
// ============================================
const autoSaveForms = document.querySelectorAll('[data-autosave]');
autoSaveForms.forEach(form => {
  const formId = form.id || 'form-' + Math.random().toString(36).substr(2, 9);
  
  // Load saved data
  const savedData = localStorage.getItem('draft-' + formId);
  if (savedData) {
    const data = JSON.parse(savedData);
    Object.keys(data).forEach(key => {
      const input = form.querySelector(`[name="${key}"]`);
      if (input) input.value = data[key];
    });
  }
  
  // Save on input
  form.addEventListener('input', debounce(function() {
    const formData = new FormData(form);
    const data = {};
    formData.forEach((value, key) => {
      data[key] = value;
    });
    localStorage.setItem('draft-' + formId, JSON.stringify(data));
  }, 500));
  
  // Clear on submit
  form.addEventListener('submit', function() {
    localStorage.removeItem('draft-' + formId);
  });
});

// ============================================
// KEYBOARD SHORTCUTS
// ============================================
document.addEventListener('keydown', function(e) {
  // Ctrl/Cmd + K: Focus search
  if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
    e.preventDefault();
    searchInput?.focus();
  }
  
  // Escape: Close modals, dropdowns
  if (e.key === 'Escape') {
    document.querySelectorAll('.modal.show').forEach(modal => {
      bootstrap.Modal.getInstance(modal)?.hide();
    });
  }
});

// ============================================
// PERFORMANCE MONITORING
// ============================================
if ('PerformanceObserver' in window) {
  const observer = new PerformanceObserver((list) => {
    for (const entry of list.getEntries()) {
      if (entry.duration > 1000) {
        console.warn('Slow operation detected:', entry.name, entry.duration + 'ms');
      }
    }
  });
  observer.observe({ entryTypes: ['measure'] });
}

// ============================================
// CONSOLE EASTER EGG
// ============================================
console.log('%c🏍️ MotoBike Store', 'font-size: 24px; font-weight: bold; color: #dc143c;');
console.log('%cChào mừng đến với MotoBike Store! 🚀', 'font-size: 14px; color: #666;');
console.log('%cNếu bạn thấy bug, hãy báo cho chúng tôi nhé! 🐛', 'font-size: 12px; color: #999;');

// ============================================
// INITIALIZATION MESSAGE
// ============================================
console.log('✅ Site.js loaded successfully');
console.log('📊 Interactive features initialized');
console.log('🎨 Animations ready');