

        // Toggle Sidebar
    const sidebar = document.getElementById('sidebar');
    const toggleBtn = document.getElementById('toggleSidebar');
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    const pageTitle = document.getElementById('pageTitle');
    const contentTitle = document.getElementById('contentTitle');

        toggleBtn.addEventListener('click', () => {
        sidebar.classList.toggle('collapsed');
        });

        // Mobile Menu
        mobileMenuBtn.addEventListener('click', () => {
        sidebar.classList.add('mobile-open');
    sidebarOverlay.classList.add('active');
        });

        sidebarOverlay.addEventListener('click', () => {
        sidebar.classList.remove('mobile-open');
    sidebarOverlay.classList.remove('active');
        });

    // Navigation Active State
    const navLinks = document.querySelectorAll('.nav-link[data-page]');
        navLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();

            // Remove active from all
            navLinks.forEach(l => l.classList.remove('active'));

            // Add active to clicked
            this.classList.add('active');

            // Update page title
            const pageName = this.getAttribute('data-page');
            pageTitle.textContent = pageName;
            contentTitle.textContent = 'صفحة ' + pageName;

            // Close mobile menu
            sidebar.classList.remove('mobile-open');
            sidebarOverlay.classList.remove('active');
        });
        });

        // Submenu Toggle
        const submenuItems = document.querySelectorAll('.nav-item.has-submenu > .nav-link');
        submenuItems.forEach(item => {
        item.addEventListener('click', function (e) {
            if (!sidebar.classList.contains('collapsed')) {
                e.preventDefault();
                this.parentElement.classList.toggle('open');
            }
        });
        });
