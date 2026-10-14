# Visualizing OS Algorithms

Đồ án cuối kỳ môn **Hệ Điều Hành**: ứng dụng WinForms (C#, .NET Framework 4.7.2) mô phỏng các thuật toán kinh điển của hệ điều hành.

## Chức năng

| Module | Thuật toán | Kết quả hiển thị |
|---|---|---|
| **CPU Scheduling** | FCFS, SJF, SRTF, Round Robin, Priority (có/không ưu tiên) | Completion / Turn-around / Waiting time, giá trị trung bình, Gantt chart (có khoảng Idle) |
| **Banker's Algorithm** | Kiểm tra trạng thái an toàn, cấp phát tài nguyên (Resource-Request) | Ma trận Need, chuỗi an toàn, phát hiện deadlock và gợi ý xử lý |
| **Page Replacement** | OPT, LRU, FIFO, Clock | Bảng trạng thái frame theo từng tham chiếu, đánh dấu page fault |
| **Disk Scheduling** | FCFS, SSTF, SCAN, C-SCAN, LOOK, C-LOOK (chọn hướng Left/Right) | Từng bước di chuyển đầu đọc và tổng quãng đường |

## Chạy chương trình

1. Mở `Project OP Final.sln` bằng Visual Studio 2019/2022 (có workload *.NET desktop development*).
2. Build và chạy (F5).

Các file dữ liệu mẫu nằm trong thư mục `Project OP Final/Data` và được copy ra cạnh file `.exe` khi build.

## Định dạng file dữ liệu

| File | Nội dung |
|---|---|
| `SRTF_CPU.txt`, `Priority_CPU.txt` | Mỗi dòng một tiến trình: `ArrivalTime BurstTime [Priority]` |
| `MaxDemand.txt`, `Allocation.txt` | Mỗi dòng một tiến trình, mỗi cột một loại tài nguyên |
| `Available.txt` | Một dòng: số tài nguyên còn trống của từng loại |
| `PageReferenceString.txt` | Chuỗi tham chiếu trang, cách nhau bởi dấu cách hoặc dấu phẩy |
| `DiskScheduling.txt` | Hàng đợi yêu cầu, cách nhau bởi dấu phẩy |

## Cấu trúc mã nguồn

```
Project OP Final/
├── Class/                 # Logic thuật toán (không phụ thuộc giao diện)
│   ├── CPUScheduling.cs
│   ├── BankerAlgorithm.cs
│   ├── PageReplacement.cs
│   ├── DiskScheduling.cs
│   └── DataFile.cs        # Hộp thoại mở/lưu file trong thư mục Data
├── Form*.cs               # Giao diện từng module
└── Data/                  # Dữ liệu mẫu
```
