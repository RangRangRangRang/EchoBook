# EchoBook - Web Reader & EPUB Management Platform

**EchoBook** là ứng dụng đọc sách điện tử (EPUB Reader) trên nền tảng Web được xây dựng nhằm tối ưu hóa trải nghiệm đọc cho người dùng với giao diện tối giản, linh hoạt và giàu tính năng.

---

## Kế Hoạch Phát Triển (Roadmap)

### Bước 1: Khung Ứng Dụng Cơ Bản (Current Baseline)
* [x] Hoàn thiện cơ bản giao diện **Library** (Thư viện) và **Upload EPUB**.
* [x] Đã thiết kế hoàn thiện giao diện **Reader UI** (Khung đọc chính, Sidebar Chương/Mục lục và Sidebar Cài đặt phông chữ, cỡ chữ, khoảng cách...).
* [x] Đã hoàn thành cơ chế tạo Key xác thực cơ bản (chưa áp dụng luồng nhập Key trên UI).

---

### Bước 2: Tối Ưu Trải Nghiệm Đọc (Focus Mode & Progress Tracking)
* [x] **Focus Mode (Chế độ tập trung):**
  * Tự động ẩn toàn bộ công cụ, tiêu đề và 2 Sidebar khi người dùng để yên con trỏ chuột trong vài giây.
  * Khi nhấn các phím điều hướng (`ArrowUp`, `ArrowDown`, `ArrowLeft`, `ArrowRight`, `Space`) để chuyển dòng/chương, ứng dụng vẫn giữ nguyên ở **Focus Mode** (không bị chớp hay hiện lại Sidebar).
  * Chỉ hiển thị lại các thanh công cụ khi con trỏ chuột di chuyển.
* [x] **Lưu vị trí đọc tự động (Auto-save Progress):**
  * Tự động lưu vết vị trí đọc hiện tại (chương và vị trí cuộn).
  * Khi đóng trang web/trình duyệt và mở lại cuốn sách đó, hệ thống tự động cuộn đến chính xác đoạn đang đọc dở.

---

### Bước 3: Luồng Người Dùng & Quản Lý Key Truy Cập
* [x] **Tự động Generate Key:**
  * Tạo Key riêng biệt cho từng người dùng/phiên làm việc.
* [x] **Cập nhật luồng giao diện khi truy cập:**
 
---

### Bước 4: AI Voice & Highlight Text
* [ ] **Hoàn thiện nâng cao Upload & Reader UI.**
* [ ] **Tích hợp AI Audio (TTS - Text-to-Speech):** Tự động đọc nội dung sách bằng giọng đọc AI sinh động.
* [ ] **Smart Highlight:** Tự động highlight (tô sáng) từng câu/đoạn văn bản tương ứng với thời điểm giọng đọc AI đang phát.

---

### Bước 5: Nâng Cấp Hệ Thống & Cơ Sở Dữ Liệu
* [ ] Chuyển đổi toàn bộ kiến trúc lưu trữ từ Docker sang **PostgreSQL Database**.
* [ ] Thêm tính năng lưu trữ tập trung file sách (EPUB/Covers) và dữ liệu tiến trình đọc người dùng trực tiếp trên Server.

---

### Bước 6: Public & Tối Ưu Hóa Cuối Cùng
* [ ] Triển khai (Deploy) ứng dụng lên môi trường Production / Public Web.
* [ ] Tối ưu hóa UI/UX toàn hệ thống, cải thiện tốc độ tải trang và phản hồi của người dùng.
* [ ] Hoàn thiện các tính năng phụ phụ trợ và sửa các lỗi phát sinh.

---

## 🛠️ Công Nghệ Sử Dụng (Tech Stack)

* **Backend:** C# / .NET Core (ASP.NET Core MVC)
* **Frontend:** HTML5, CSS3, JavaScript (ES6+), Bootstrap
* **Styling:** Custom CSS Flexbox/Grid (`site.css`)
* **Storage / Database:** LocalStorage, PostgreSQL (Planned)

---

## Hướng Dẫn Cài Đặt & Chạy Cục Bộ (Local Setup)

1. **Clone repository:**
   ```bash
   git clone [https://github.com/your-username/EchoBook.git](https://github.com/your-username/EchoBook.git)
