# Chat Application (Server + WPF Client)

Ứng dụng chat đơn giản gồm 2 phần:
- **ChatServer** (Console App)
- **ChatClientWPF** (WPF App)

---

## 🚀 Hướng dẫn chạy

### 1. Mở solution
- Mở file **`MyChatSolution.sln`** trong Visual Studio.  
- Solution gồm:
  - `ChatServer`
  - `ChatClientWPF`

---

### 2. Chạy server
1. Chuột phải vào project **ChatServer** → **Set as Startup Project**  
2. Nhấn **`Ctrl + F5`** để chạy  
3. Server mặc định lắng nghe ở:  
127.0.0.1:5000

markdown
Sao chép mã

---

### 3. Chạy client (WPF)
1. Chuột phải vào **ChatClientWPF** → **Set as Startup Project**  
2. Nhấn **`Ctrl + F5`** để chạy  
3. Nhập **IP** và **Port** để kết nối  

- Nếu chạy trên **cùng máy**:
IP: 127.0.0.1
Port: 5000

markdown
Sao chép mã

- Nếu chạy trên **2 máy trong LAN**:
- Trên máy server, mở **Command Prompt** và gõ:
  ```bash
  ipconfig
  ```
- Lấy **IPv4 Address** (ví dụ `192.168.1.5`)  
- Trên client nhập IP này và port `5000`

---

### 4. Chạy song song trong Visual Studio
1. Vào **Solution Properties** → **Startup Project**  
2. Chọn **Multiple startup projects**  
3. Đặt `ChatServer` và `ChatClientWPF` → **Start**  
4. Nhấn **`Ctrl + F5`**, cả 2 sẽ chạy cùng lúc  
