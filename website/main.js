/* ===================================================================
   LUMOS — Landing Page Interactions
   Particles, scroll animations, brightness demo, navigation
   =================================================================== */

document.addEventListener('DOMContentLoaded', () => {
  initParticles();
  initScrollReveal();
  initNavigation();
  initBrightnessDemo();
  initBackToTop();
  initSmoothScroll();
  initCountUp();
});

/* --- Floating Light Particles --- */
function initParticles() {
  const container = document.getElementById('particles');
  if (!container) return;

  const PARTICLE_COUNT = 30;

  for (let i = 0; i < PARTICLE_COUNT; i++) {
    const particle = document.createElement('div');
    particle.className = 'particle';

    const size = Math.random() * 3 + 1.5;
    const left = Math.random() * 100;
    const duration = Math.random() * 12 + 10;
    const delay = Math.random() * 15;
    const opacity = Math.random() * 0.4 + 0.1;

    particle.style.cssText = `
      width: ${size}px;
      height: ${size}px;
      left: ${left}%;
      animation-duration: ${duration}s;
      animation-delay: ${delay}s;
      opacity: 0;
      filter: blur(${size > 3 ? 1 : 0}px);
    `;

    // Randomly tint some particles slightly different
    if (Math.random() > 0.6) {
      particle.style.background = '#fcd34d';
    }

    container.appendChild(particle);
  }
}

/* --- Scroll Reveal Observer --- */
function initScrollReveal() {
  const reveals = document.querySelectorAll('.reveal');
  if (!reveals.length) return;

  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add('visible');
          // Optionally unobserve after reveal for performance
          observer.unobserve(entry.target);
        }
      });
    },
    {
      threshold: 0.12,
      rootMargin: '0px 0px -40px 0px',
    }
  );

  reveals.forEach((el) => observer.observe(el));
}

/* --- Navigation: Scroll Shrink + Mobile Toggle --- */
function initNavigation() {
  const nav = document.getElementById('nav');
  const toggle = document.getElementById('navToggle');
  const links = document.getElementById('navLinks');

  if (!nav) return;

  // Scroll effect
  let lastScroll = 0;
  const handleScroll = () => {
    const currentScroll = window.pageYOffset;
    nav.classList.toggle('scrolled', currentScroll > 32);
    lastScroll = currentScroll;
  };

  window.addEventListener('scroll', handleScroll, { passive: true });
  handleScroll(); // Initial check

  // Mobile toggle
  if (toggle && links) {
    toggle.addEventListener('click', () => {
      const isOpen = links.classList.toggle('open');
      toggle.setAttribute('aria-expanded', isOpen);

      // Animate hamburger to X
      const spans = toggle.querySelectorAll('span');
      if (isOpen) {
        spans[0].style.transform = 'rotate(45deg) translate(5px, 5px)';
        spans[1].style.opacity = '0';
        spans[2].style.transform = 'rotate(-45deg) translate(5px, -5px)';
      } else {
        spans[0].style.transform = '';
        spans[1].style.opacity = '';
        spans[2].style.transform = '';
      }
    });

    // Close on link click
    links.querySelectorAll('.nav__link').forEach((link) => {
      link.addEventListener('click', () => {
        links.classList.remove('open');
        toggle.setAttribute('aria-expanded', false);
        const spans = toggle.querySelectorAll('span');
        spans[0].style.transform = '';
        spans[1].style.opacity = '';
        spans[2].style.transform = '';
      });
    });
  }
}

/* --- Interactive Brightness Demo --- */
function initBrightnessDemo() {
  const demo = document.getElementById('brightnessDemo');
  if (!demo) return;

  const apps = demo.querySelectorAll('.brightness-demo__app');
  let currentIndex = 0;
  let demoStarted = false;

  // Intersection observer to start demo when visible
  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting && !demoStarted) {
          demoStarted = true;
          startDemoCycle();
        }
      });
    },
    { threshold: 0.3 }
  );

  observer.observe(demo);

  function animateApp(app, targetBrightness) {
    const fill = app.querySelector('.brightness-demo__slider-fill');
    const value = app.querySelector('.brightness-demo__value');

    if (!fill || !value) return;

    // Animate the fill bar
    fill.style.width = targetBrightness + '%';

    // Animate the number
    const startVal = parseInt(value.textContent) || 0;
    const diff = targetBrightness - startVal;
    const duration = 1500;
    const startTime = performance.now();

    function updateValue(now) {
      const elapsed = now - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3); // easeOutCubic
      const current = Math.round(startVal + diff * eased);
      value.textContent = current + '%';

      if (progress < 1) {
        requestAnimationFrame(updateValue);
      }
    }

    requestAnimationFrame(updateValue);

    // Highlight active app via CSS class
    apps.forEach((a) => a.classList.remove('active'));
    app.classList.add('active');
  }

  function startDemoCycle() {
    function cycleNext() {
      const app = apps[currentIndex];
      if (!app) return;

      const brightness = parseInt(app.dataset.brightness) || 50;
      animateApp(app, brightness);

      currentIndex = (currentIndex + 1) % apps.length;
      setTimeout(cycleNext, 2200);
    }

    cycleNext();
  }
}

/* --- Back to Top Button --- */
function initBackToTop() {
  const btn = document.getElementById('backToTop');
  if (!btn) return;

  const handleScroll = () => {
    btn.classList.toggle('visible', window.pageYOffset > 400);
  };

  window.addEventListener('scroll', handleScroll, { passive: true });

  btn.addEventListener('click', () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  });
}

/* --- Smooth Scroll for Anchor Links --- */
function initSmoothScroll() {
  document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
    anchor.addEventListener('click', (e) => {
      const targetId = anchor.getAttribute('href');
      if (targetId === '#') return;

      const target = document.querySelector(targetId);
      if (target) {
        e.preventDefault();
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }
    });
  });
}

/* --- Count-Up Animation for Stats --- */
function initCountUp() {
  const statValues = document.querySelectorAll('.hero__stat-value');
  if (!statValues.length) return;

  // These are text-based stats, so we'll add a subtle entrance animation
  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.style.animation = 'fadeSlideUp 0.6s ease-out both';
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.5 }
  );

  statValues.forEach((el) => observer.observe(el));
}

/* --- Subtle parallax on hero visual --- */
document.addEventListener('mousemove', (e) => {
  const visual = document.querySelector('.hero__visual-img');
  if (!visual) return;

  const rect = visual.getBoundingClientRect();
  if (rect.bottom < 0 || rect.top > window.innerHeight) return;

  const centerX = window.innerWidth / 2;
  const centerY = window.innerHeight / 2;
  const moveX = (e.clientX - centerX) / centerX;
  const moveY = (e.clientY - centerY) / centerY;

  visual.style.transform = `perspective(1000px) rotateY(${moveX * 2}deg) rotateX(${-moveY * 2}deg)`;
});

/* --- Keyboard accessibility: Enter activates buttons --- */
document.querySelectorAll('[role="button"]').forEach((el) => {
  el.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      el.click();
    }
  });
});
