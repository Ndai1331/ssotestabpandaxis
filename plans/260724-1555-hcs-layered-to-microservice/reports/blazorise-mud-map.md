# Blazorise → MudBlazor migration map

Port theo luồng nghiệp vụ, không yêu cầu pixel-perfect. Ưu tiên component MudBlazor hiện có trong `apps/blazor`.

| Blazorise | MudBlazor | Ghi chú port |
|---|---|---|
| `DataGrid` | `MudDataGrid` | Dùng `ServerData` cho bảng lớn; giữ sort/filter/page phía API. |
| `Table` | `MudTable` | Chỉ dùng cho danh sách đơn giản. |
| `Modal` | `MudDialog` | Tách form thành dialog component, trả `DialogResult`. |
| `Validations` | `MudForm` | DTO validation vẫn là nguồn chuẩn; gọi `Validate()` trước submit. |
| `TextEdit` | `MudTextField` | Gán `For` khi có model expression. |
| `NumericEdit` | `MudNumericField` | Khai báo kiểu nullable đúng DTO. |
| `DateEdit` | `MudDatePicker` | Chuẩn hóa timezone ở application service. |
| `TimeEdit` | `MudTimePicker` | Không lưu local time không kèm ngữ cảnh timezone. |
| `Select` | `MudSelect` | Dùng `Item` hoặc render fragment cho lookup. |
| `Autocomplete` | `MudAutocomplete` | Search server-side, debounce cho danh mục lớn. |
| `Check` | `MudCheckBox` | Dùng `T` rõ ràng cho nullable bool. |
| `RadioGroup` | `MudRadioGroup` | Enum hiển thị qua localization. |
| `Tabs` | `MudTabs` | Không tải dữ liệu tab ẩn nếu tốn chi phí. |
| `Steps` | `MudStepper` | Validate từng bước trước chuyển bước. |
| `Card` | `MudCard` | Giữ hierarchy semantic, tránh card lồng sâu. |
| `Badge` | `MudChip` | Map màu trạng thái qua một helper dùng chung. |
| `Alert` | `MudAlert` | Lỗi nghiệp vụ chi tiết vẫn qua exception handling ABP. |
| `Tooltip` | `MudTooltip` | Không dùng tooltip thay label bắt buộc. |
| `Dropdown` | `MudMenu` | Action nguy hiểm phải có confirm dialog. |
| `FileEdit` | `MudFileUpload` | Kiểm tra size/MIME ở cả client và DocumentService. |
| `Pagination` | `MudPagination` | Page index UI 1-based, API `SkipCount` 0-based. |
| `Bar/LineChart` | `MudChart` | Reporting nâng cao có thể dùng chart library riêng ở P7. |
| `Toast` | `ISnackbar` | Không hiển thị secret/stack trace. |
| `LoadingIndicator` | `MudProgressCircular` | Disable action trong khi request đang chạy. |

## Page port checklist

1. Giữ route và permission behavior tương đương.
2. Chuyển API call sang service contract/dynamic HTTP proxy.
3. Giữ loading, empty, error và unauthorized state.
4. Kiểm thử keyboard, label, focus dialog và responsive layout.
5. Đánh dấu feature `UI`, sau E2E mới chuyển `Verified`.
