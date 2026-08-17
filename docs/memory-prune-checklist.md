# Memory Prune Checklist

Chạy định kỳ mỗi **2–4 tuần** để giữ memory system clean và accurate.

## Cách chạy audit

```bash
bash /Users/user/Desktop/projects/task9-workspace/.claude/hooks/memory-audit.sh
```

## Checklist

### 1. Scan for stale entries

Đọc từng entry trong `MEMORY.md` và kiểm tra:

- [ ] Server/service đề cập có còn tồn tại không? (vd: do-16 → đã xóa 2026-05-14)
- [ ] Constraint còn valid với code hiện tại không?
- [ ] `project-*` entries — trạng thái dự án có còn đúng không?

Đánh dấu `[STALE]` những entry không còn áp dụng.

### 2. Merge duplicates

- [ ] Có ≥2 feedback entries về cùng chủ đề (vd: deploy) không?
- [ ] Nếu có → tạo `pattern-*.md` mới tổng hợp, xóa các feedback entries đã generalize

### 3. Enforce limits

- [ ] `MEMORY.md` ≤ 20 entries?
- [ ] Mỗi memory file ≤ 40 dòng?
- [ ] Nếu vượt → prune `project-*` entries cũ nhất (stale nhanh nhất)

### 4. Update index

- [ ] Xóa `[STALE]` entries khỏi `MEMORY.md` (giữ file gốc thêm 1 sprint để rollback)
- [ ] Verify mọi link trong `MEMORY.md` trỏ đúng file
- [ ] Tùy chọn: chạy `/anthropic-skills:consolidate-memory` để AI tự consolidate

## Memory dir

```
~/.claude/projects/-Users-user-Desktop-projects-task9-workspace/memory/
```

## Taxonomy hiện tại

| Prefix | Ý nghĩa | Prune priority |
|--------|---------|----------------|
| `constraint-*` | Ràng buộc cứng hệ thống | Thấp (thường ổn định) |
| `feedback-*` | Lỗi behavior đã gặp | Trung bình |
| `project-*` | Trạng thái dự án | **Cao** (stale nhanh) |
| `pattern-*` | Best practice đã validate | Thấp |
