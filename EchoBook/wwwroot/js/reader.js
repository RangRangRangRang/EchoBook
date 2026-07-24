document.addEventListener('DOMContentLoaded', () => {
    const bootstrapElement = document.getElementById('reader-bootstrap-data');
    if (!bootstrapElement) return;

    let readerData = {};
    try {
        readerData = JSON.parse(bootstrapElement.textContent);
    } catch (e) {
        console.error("Lỗi parse bootstrap data:", e);
        return;
    }

    const readerApp = document.getElementById('reader-app');
    const readerViewport = document.getElementById('reader-viewport');
    const readerColumns = document.getElementById('reader-columns');
    const bookTitleEl = document.getElementById('reader-book-title');

    const panelChapters = document.getElementById('panel-chapters');
    const panelBookmarks = document.getElementById('panel-bookmarks');

    // Controls
    const settingFontSize = document.getElementById('setting-font-size');
    const settingFontSizeVal = document.getElementById('setting-font-size-val');
    const settingLineHeight = document.getElementById('setting-line-height');
    const settingLineHeightVal = document.getElementById('setting-line-height-val');
    const settingLetterSpacing = document.getElementById('setting-letter-spacing');
    const settingLetterSpacingVal = document.getElementById('setting-letter-spacing-val');
    const settingFont = document.getElementById('setting-font');
    const settingLanguage = document.getElementById('setting-language');
    const settingDarkMode = document.getElementById('setting-dark-mode');

    let currentChapterIndex = 0;

    // Bookmark Storage
    const storageKey = `echobook_bm_${readerData.bookId || 'default'}`;
    let bookmarks = JSON.parse(localStorage.getItem(storageKey) || '[]');

    const translations = {
        en: {
            backToLibrary: "← Library",
            chapters: "Chapters",
            bookmarks: "Bookmarks",
            addBookmark: "+ Bookmark Current Position",
            noBookmarks: "No bookmarks saved.",
            darkMode: "Dark Mode",
            language: "Language",
            font: "Font",
            fontSize: "Font Size",
            lineHeight: "Line Height",
            letterSpacing: "Letter Spacing",
            noContent: "This chapter has no content."
        },
        vi: {
            backToLibrary: "← Thư viện",
            chapters: "Mục lục",
            bookmarks: "Dấu trang",
            addBookmark: "+ Đánh dấu vị trí này",
            noBookmarks: "Chưa có dấu trang nào.",
            darkMode: "Chế độ tối",
            language: "Ngôn ngữ",
            font: "Phông chữ",
            fontSize: "Cỡ chữ",
            lineHeight: "Khoảng cách dòng",
            letterSpacing: "Khoảng cách chữ",
            noContent: "Chương này không có nội dung."
        }
    };

    function init() {
        if (readerData.title && bookTitleEl) {
            bookTitleEl.textContent = readerData.title;
        }

        renderChaptersList();
        renderBookmarksList();
        loadSettings();

        if (readerData.chapters && readerData.chapters.length > 0) {
            loadChapter(0);
        }

        setupEventListeners();
    }

    function applyLanguage(lang) {
        const dict = translations[lang] || translations.en;
        document.querySelectorAll('[data-i18n]').forEach(el => {
            const key = el.getAttribute('data-i18n');
            if (dict[key]) el.textContent = dict[key];
        });
        renderBookmarksList();
    }

    function renderChaptersList() {
        if (!panelChapters || !readerData.chapters) return;
        panelChapters.innerHTML = '';
        readerData.chapters.forEach((ch, idx) => {
            const li = document.createElement('li');
            li.className = `sidebar-list-item ${idx === currentChapterIndex ? 'active' : ''}`;
            li.textContent = ch.title || `Chương ${idx + 1}`;
            li.addEventListener('click', () => loadChapter(idx));
            panelChapters.appendChild(li);
        });
    }

    function renderBookmarksList() {
        if (!panelBookmarks) return;
        panelBookmarks.innerHTML = '';

        const currentLang = settingLanguage ? settingLanguage.value : 'en';
        const dict = translations[currentLang] || translations.en;

        const addLi = document.createElement('li');
        addLi.className = 'sidebar-list-item';
        addLi.style.cssText = 'font-weight:600; color:#00c9a7; border:1px dashed var(--eb-border); margin-bottom:8px;';
        addLi.textContent = dict.addBookmark;
        addLi.addEventListener('click', addCurrentBookmark);
        panelBookmarks.appendChild(addLi);

        if (bookmarks.length === 0) {
            const emptyLi = document.createElement('li');
            emptyLi.className = 'sidebar-list-item sidebar-list-empty';
            emptyLi.textContent = dict.noBookmarks;
            panelBookmarks.appendChild(emptyLi);
            return;
        }

        bookmarks.forEach((bm, idx) => {
            const li = document.createElement('li');
            li.className = 'sidebar-list-item';

            const span = document.createElement('span');
            span.textContent = `${bm.chapterTitle} (${Math.round(bm.scrollPos)}px)`;
            span.style.cssText = 'overflow:hidden; text-overflow:ellipsis; white-space:nowrap; flex:1;';
            span.addEventListener('click', () => {
                if (currentChapterIndex !== bm.chapterIndex) {
                    loadChapter(bm.chapterIndex, bm.scrollPos);
                } else if (readerViewport) {
                    readerViewport.scrollTop = bm.scrollPos;
                }
            });

            const delBtn = document.createElement('button');
            delBtn.className = 'bookmark-delete';
            delBtn.innerHTML = '&times;';
            delBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                bookmarks.splice(idx, 1);
                localStorage.setItem(storageKey, JSON.stringify(bookmarks));
                renderBookmarksList();
            });

            li.appendChild(span);
            li.appendChild(delBtn);
            panelBookmarks.appendChild(li);
        });
    }

    function addCurrentBookmark() {
        const chapter = readerData.chapters[currentChapterIndex];
        const title = chapter ? (chapter.title || `Chương ${currentChapterIndex + 1}`) : 'Bookmark';
        const scrollPos = readerViewport ? readerViewport.scrollTop : 0;

        bookmarks.push({
            chapterIndex: currentChapterIndex,
            chapterTitle: title,
            scrollPos: scrollPos
        });

        localStorage.setItem(storageKey, JSON.stringify(bookmarks));
        renderBookmarksList();
    }

    async function loadChapter(index, targetScroll = 0) {
        if (index < 0 || index >= readerData.chapters.length) return;

        currentChapterIndex = index;
        if (readerApp) readerApp.classList.add('reader-loading');

        const chapter = readerData.chapters[index];
        if (bookTitleEl) {
            bookTitleEl.textContent = `${readerData.title} — ${chapter.title || ''}`;
        }

        let htmlContent = chapter.html || chapter.htmlContent || chapter.content || '';

        if (!htmlContent && chapter.id && readerData.bookId) {
            try {
                const res = await fetch(`/Reader/${readerData.bookId}/Chapter/${chapter.id}`);
                if (res.ok) {
                    const data = await res.json();
                    htmlContent = data.html || data.htmlContent || data.content || '';
                    chapter.html = htmlContent;
                }
            } catch (err) {
                console.error("Lỗi fetch chương:", err);
            }
        }

        const currentLang = settingLanguage ? settingLanguage.value : 'en';
        const noContentText = translations[currentLang]?.noContent || translations.en.noContent;

        if (readerColumns) {
            readerColumns.innerHTML = htmlContent || `<div class="text-center p-4">${noContentText}</div>`;
        }

        // Tự chuyển cuộn: nếu targetScroll === 'bottom' thì cuộn xuống cuối trang
        setTimeout(() => {
            if (readerViewport) {
                if (targetScroll === 'bottom') {
                    readerViewport.scrollTop = readerViewport.scrollHeight - readerViewport.clientHeight;
                } else {
                    readerViewport.scrollTop = targetScroll;
                }
            }
            if (readerApp) readerApp.classList.remove('reader-loading');
        }, 80);

        if (panelChapters) {
            const items = panelChapters.querySelectorAll('.sidebar-list-item');
            items.forEach((item, i) => item.classList.toggle('active', i === index));
        }
    }

    function loadSettings() {
        const defaultFontSize = 18;
        const defaultLineHeight = 1.7;
        const defaultLetterSpacing = 0.05;

        if (settingFontSize && readerColumns) {
            settingFontSize.value = defaultFontSize;
            if (settingFontSizeVal) settingFontSizeVal.textContent = `${defaultFontSize}px`;
            readerColumns.style.fontSize = `${defaultFontSize}px`;
        }

        if (settingLineHeight && readerColumns) {
            settingLineHeight.value = defaultLineHeight;
            if (settingLineHeightVal) settingLineHeightVal.textContent = defaultLineHeight;
            readerColumns.style.lineHeight = defaultLineHeight;
        }

        if (settingLetterSpacing && readerColumns) {
            settingLetterSpacing.value = defaultLetterSpacing;
            if (settingLetterSpacingVal) settingLetterSpacingVal.textContent = `${defaultLetterSpacing}em`;
            readerColumns.style.letterSpacing = `${defaultLetterSpacing}em`;
        }

        if (settingLanguage) applyLanguage(settingLanguage.value);
    }

    function setupEventListeners() {
        // ĐIỀU HƯỚNG PHÍM BẤM & TỰ CHUYỂN CHAPTER THÔNG MINH
        document.addEventListener('keydown', (e) => {
            if (!readerViewport) return;

            const isAtTop = readerViewport.scrollTop <= 5;
            const isAtBottom = (readerViewport.scrollTop + readerViewport.clientHeight) >= (readerViewport.scrollHeight - 10);

            // Mũi tên Xuống
            if (e.code === 'ArrowDown') {
                e.preventDefault();
                readerViewport.scrollTop += 40;
            }
            // Mũi tên Lên
            else if (e.code === 'ArrowUp') {
                e.preventDefault();
                if (isAtTop && currentChapterIndex > 0) {
                    loadChapter(currentChapterIndex - 1, 'bottom');
                } else {
                    readerViewport.scrollTop -= 40;
                }
            }
            // Phím Mũi Tên Phải / Space / PageDown -> Cuộn trang hoặc Sang Chapter sau
            else if (['ArrowRight', 'Space', 'PageDown'].includes(e.code)) {
                e.preventDefault();
                if (isAtBottom && currentChapterIndex < readerData.chapters.length - 1) {
                    loadChapter(currentChapterIndex + 1, 0); // Sang đầu chapter sau
                } else {
                    readerViewport.scrollTop += (readerViewport.clientHeight - 60);
                }
            }
            // Phím Mũi Tên Trái / PageUp -> Cuộn ngược lại hoặc Sang cuối Chapter trước
            else if (['ArrowLeft', 'PageUp'].includes(e.code)) {
                e.preventDefault();
                if (isAtTop && currentChapterIndex > 0) {
                    loadChapter(currentChapterIndex - 1, 'bottom'); // Sang cuối chapter trước
                } else {
                    readerViewport.scrollTop -= (readerViewport.clientHeight - 60);
                }
            }
        });

        // Dark Mode Switcher
        if (settingDarkMode) {
            settingDarkMode.addEventListener('change', (e) => {
                document.body.classList.toggle('theme-light', !e.target.checked);
            });
        }

        // Language Change
        if (settingLanguage) {
            settingLanguage.addEventListener('change', (e) => applyLanguage(e.target.value));
        }

        // Toggle Sidebar Panels
        document.querySelectorAll('.sidebar-toggle').forEach(btn => {
            btn.addEventListener('click', () => {
                const targetId = btn.getAttribute('data-panel');
                const panel = document.getElementById(targetId);
                if (panel) panel.classList.toggle('open');
            });
        });

        if (settingFontSize) {
            settingFontSize.addEventListener('input', (e) => {
                const val = e.target.value;
                if (settingFontSizeVal) settingFontSizeVal.textContent = `${val}px`;
                if (readerColumns) readerColumns.style.fontSize = `${val}px`;
            });
        }

        if (settingLineHeight) {
            settingLineHeight.addEventListener('input', (e) => {
                const val = e.target.value;
                if (settingLineHeightVal) settingLineHeightVal.textContent = val;
                if (readerColumns) readerColumns.style.lineHeight = val;
            });
        }

        if (settingLetterSpacing) {
            settingLetterSpacing.addEventListener('input', (e) => {
                const val = e.target.value;
                if (settingLetterSpacingVal) settingLetterSpacingVal.textContent = `${val}em`;
                if (readerColumns) readerColumns.style.letterSpacing = `${val}em`;
            });
        }

        if (settingFont) {
            settingFont.addEventListener('change', (e) => {
                if (readerColumns) readerColumns.style.fontFamily = e.target.value;
            });
        }
    }

    init();
});