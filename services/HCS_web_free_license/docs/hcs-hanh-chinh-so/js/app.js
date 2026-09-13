(function () {
  "use strict";

  var i18n = {
    vi: {
      account: "Tài khoản HCS",
      chooseLang: "Chọn ngôn ngữ",
      username: "Tên đăng nhập hoặc địa chỉ email",
      password: "Mật khẩu",
      remember: "Ghi nhớ",
      login: "Đăng nhập",
      orLogin: "Hoặc đăng nhập bằng:",
      sso: "Login với SSO",
      userRequired: "Vui lòng nhập tên đăng nhập hoặc email.",
      passRequired: "Vui lòng nhập mật khẩu.",
      showPassword: "Hiện mật khẩu",
      hidePassword: "Ẩn mật khẩu",
      ssoTitle: "Đăng nhập SSO",
      ssoBody: "Đang chuyển hướng tới nhà cung cấp xác thực...",
      searchDone: "Đã lọc workspace theo khoảng ngày đã chọn.",
      docsFiltered: "Đã lọc danh sách văn bản.",
      docsRefreshed: "Đã làm mới danh sách văn bản.",
      docsSaved: "Đã lưu văn bản.",
      docsNeedTitle: "Nhập tiêu đề văn bản.",
      docsNeedSave: "Lưu văn bản trước khi trình, ký hoặc gửi.",
      docsNeedPresent: "Trình văn bản trước khi gửi.",
      docsAssigned: "Đã phân công người tham gia.",
      docsNeedPerson: "Chọn người tham gia.",
      docsPresented: "Đã trình văn bản.",
      docsSent: "Đã gửi văn bản.",
      docsFilePicked: "Đã chọn tệp đính kèm.",
      signNeedWf: "Chọn quy trình để trình ký.",
      signSubmitted: "Đã tạo trình ký.",
      wfFiltered: "Đã lọc danh sách quy trình.",
      wfRefreshed: "Đã làm mới danh sách quy trình.",
      wfSaved: "Đã lưu quy trình.",
      wfDone: "Đã hoàn tất quy trình.",
      wfStepSaved: "Đã lưu bước quy trình.",
      wfStepDeleted: "Đã xóa bước.",
      wfStepNeedName: "Nhập tên bước.",
      wfNeedPerson: "Chọn người thực hiện.",
      wfFilePicked: "Đã chọn tệp mẫu.",
      pjFiltered: "Đã lọc danh sách dự án.",
      dmFiltered: "Đã lọc danh mục phòng ban.",
      dmSaved: "Đã lưu phòng ban.",
      dmDeleted: "Đã xóa phòng ban.",
      dmNeedCode: "Nhập mã và tên phòng ban.",
      cvFiltered: "Đã lọc danh sách công việc.",
      cvMoved: "Đã chuyển trạng thái công việc.",
      queueFiltered: "Đã lọc hàng đợi ký.",
      queueRefreshed: "Đã làm mới hàng đợi ký.",
      chatRefreshed: "Đã làm mới hội thoại.",
      chatEmpty: "Vui lòng nhập tin nhắn.",
      chatRenamed: "Đã lưu tên hội thoại.",
      chatNoPhotos: "Chưa có ảnh.",
      chatNoFiles: "Chưa có tệp.",
      chatPinned: "Đã ghim tin nhắn.",
      chatUnpinned: "Đã bỏ ghim tin nhắn.",
      chatPinnedConv: "Đã ghim hội thoại.",
      chatUnpinnedConv: "Đã bỏ ghim hội thoại.",
      chatRecalled: "Đã thu hồi tin nhắn.",
      chatDeleted: "Đã xóa tin nhắn.",
      chatForwarded: "Đã chuyển tiếp.",
      chatTaskCreated: "Đã giao việc.",
      chatNoTarget: "Chọn hội thoại để chuyển tiếp.",
      chatTaskNeed: "Nhập tên công việc và người thực hiện.",
      chatNoPins: "Chưa ghim tin nhắn.",
      chatRecallCopy: "Thu hồi tin nhắn này? Mọi người trong hội thoại sẽ không còn thấy nội dung.",
      chatDeleteCopy: "Xóa tin nhắn này khỏi máy của bạn?",
      chatNeedContact: "Chọn ít nhất một người trong danh bạ.",
      chatNeedGroupName: "Nhập tên nhóm để tạo cuộc trao đổi nhóm.",
      chatCreatedDm: "Đã tạo trao đổi riêng.",
      chatCreatedGroup: "Đã tạo nhóm trò chuyện.",
      chatMemberRemoved: "Đã xóa thành viên khỏi nhóm.",
      socialPosted: "Đã đăng bài viết.",
      socialEmptyPost: "Nhập nội dung bài viết.",
      socialCommented: "Đã gửi bình luận.",
      socialShared: "Đã sao chép liên kết bài viết.",
      socialFiltered: "Đã lọc bài viết.",
      socialCleared: "Đã xóa bộ lọc.",
      socialReacted: "Đã bày tỏ cảm xúc.",
      eventSaved: "Đã lưu sự kiện.",
      eventCreated: "Đã tạo sự kiện mới.",
      eventDeleted: "Đã xóa sự kiện.",
      eventRefreshed: "Đã làm mới dashboard sự kiện.",
      eventFiltered: "Đã lọc danh sách sự kiện.",
      eventReset: "Đã đặt lại bộ lọc.",
      eventGuestAdded: "Đã thêm người tham dự.",
      eventGuestDeleted: "Đã xóa người tham dự.",
      eventGuestNeedSelect: "Chọn ít nhất một người tham dự để xóa.",
      eventGuestNeedPick: "Chọn ít nhất một người dùng để thêm.",
      eventFileAttached: "Đã đính kèm tài liệu.",
      eventFileDeleted: "Đã xóa tài liệu.",
      eventCheckinOk: "Đã check-in thành công.",
      eventCheckinDone: "Bạn đã check-in sự kiện này.",
      eventCheckinHint: "Bạn đã đăng nhập. Xác nhận tham dự trước khi check-in tại sự kiện.",
      eventCheckinBtn: "Xác nhận tham dự",
      eventCheckinBtnDone: "Đã check-in",
      eventRsvpOk: "Đã xác nhận tham dự.",
      eventRsvpDone: "Bạn đã xác nhận tham dự. Check-in khi sự kiện bắt đầu và bạn có mặt.",
      eventRsvpWait: "Bạn đã xác nhận tham dự. Sự kiện chưa bắt đầu — check-in sẽ mở khi đến giờ.",
      eventCheckinReady: "Sự kiện đang diễn ra. Nhấn check-in để ghi nhận có mặt.",
      eventCheckinBtnGo: "Check-in có mặt",
      eventCheckinEnded: "Sự kiện đã kết thúc. Không thể check-in thêm.",
      eventCheckinNeedLogin: "Đăng nhập để xác nhận tham dự sự kiện.",
      eventCheckinLoginOk: "Đăng nhập thành công.",
      eventCheckinNeedUser: "Nhập tên đăng nhập và mật khẩu.",
      eventCheckinKickerLogin: "Đăng nhập",
      eventCheckinKickerRsvp: "Xác nhận tham dự",
      eventCheckinKickerCheck: "Check-in",
      calFiltered: "Đã lọc lịch công tác.",
      calReset: "Đã đặt lại lịch.",
      openMenu: "Mở menu",
      closeMenu: "Đóng menu",
      notifications: "Thông báo",
      messages: "Tin nhắn",
      markAllRead: "Đánh dấu đã đọc",
      notifyEmpty: "Không có thông báo.",
      viewNotify: "Xem tất cả thông báo",
      langVi: "Tiếng Việt",
      langEn: "English",
      notifyRead: "Đã đánh dấu tất cả là đã đọc.",
      nSignTitle: "3 văn bản chờ ký duyệt",
      nSignBody: "Hạn trong ngày · Trình ký",
      nChatTitle: "Tin nhắn mới",
      nChatBody: "nguyễn hồ phi long vừa gửi tin",
      nEventTitle: "Họp giao ban tuần",
      nEventBody: "Bắt đầu 09:00 ngày 11/09",
      nSocialTitle: "Bình luận bài viết",
      nSocialBody: "Phó trưởng phòng đã trả lời bạn",
      nDocTitle: "Công văn đến mới",
      nDocBody: "Văn bản nội bộ đã được ban hành",
      pdfDownload: "Tải PDF",
      pdfClose: "Đóng",
      pdfPage: "1 / 1"
    },
    en: {
      account: "HCS account",
      chooseLang: "Language",
      username: "Username or email address",
      password: "Password",
      remember: "Remember me",
      login: "Sign in",
      orLogin: "Or sign in with:",
      sso: "Login with SSO",
      userRequired: "Please enter your username or email.",
      passRequired: "Please enter your password.",
      showPassword: "Show password",
      hidePassword: "Hide password",
      ssoTitle: "SSO sign-in",
      ssoBody: "Redirecting to the identity provider...",
      searchDone: "Workspace filtered by the selected date range.",
      docsFiltered: "Document list filtered.",
      docsRefreshed: "Document list refreshed.",
      docsSaved: "Document saved.",
      docsNeedTitle: "Enter a document title.",
      docsNeedSave: "Save the document before presenting, signing, or sending.",
      docsNeedPresent: "Present the document before sending.",
      docsAssigned: "Participant assigned.",
      docsNeedPerson: "Choose a participant.",
      docsPresented: "Document submitted.",
      docsSent: "Document sent.",
      docsFilePicked: "Attachment selected.",
      signNeedWf: "Select a workflow to submit for signing.",
      signSubmitted: "Signing request created.",
      wfFiltered: "Process list filtered.",
      wfRefreshed: "Process list refreshed.",
      wfSaved: "Process saved.",
      wfDone: "Process completed.",
      wfStepSaved: "Process step saved.",
      wfStepDeleted: "Step removed.",
      wfStepNeedName: "Enter a step name.",
      wfNeedPerson: "Choose an assignee.",
      wfFilePicked: "Template file selected.",
      pjFiltered: "Project list filtered.",
      dmFiltered: "Department catalog filtered.",
      dmSaved: "Department saved.",
      dmDeleted: "Department removed.",
      dmNeedCode: "Enter a department code and name.",
      cvFiltered: "Task list filtered.",
      cvMoved: "Task status updated.",
      queueFiltered: "Signing queue filtered.",
      queueRefreshed: "Signing queue refreshed.",
      chatRefreshed: "Conversations refreshed.",
      chatEmpty: "Please enter a message.",
      chatRenamed: "Conversation name saved.",
      chatNoPhotos: "No photos yet.",
      chatNoFiles: "No files yet.",
      chatPinned: "Message pinned.",
      chatUnpinned: "Message unpinned.",
      chatPinnedConv: "Conversation pinned.",
      chatUnpinnedConv: "Conversation unpinned.",
      chatRecalled: "Message recalled.",
      chatDeleted: "Message deleted.",
      chatForwarded: "Forwarded.",
      chatTaskCreated: "Task assigned.",
      chatNoTarget: "Choose a conversation to forward to.",
      chatTaskNeed: "Enter a task title and assignee.",
      chatNoPins: "No pinned messages.",
      chatRecallCopy: "Recall this message? Others will no longer see its content.",
      chatDeleteCopy: "Delete this message from your device?",
      chatNeedContact: "Select at least one contact.",
      chatNeedGroupName: "Enter a group name to start a group chat.",
      chatCreatedDm: "Direct conversation created.",
      chatCreatedGroup: "Group conversation created.",
      chatMemberRemoved: "Member removed from the group.",
      socialPosted: "Post published.",
      socialEmptyPost: "Please enter post content.",
      socialCommented: "Comment sent.",
      socialShared: "Post link copied.",
      socialFiltered: "Posts filtered.",
      socialCleared: "Filters cleared.",
      socialReacted: "Reaction saved.",
      eventSaved: "Event saved.",
      eventCreated: "Event created.",
      eventDeleted: "Event deleted.",
      eventRefreshed: "Event dashboard refreshed.",
      eventFiltered: "Event list filtered.",
      eventReset: "Filters reset.",
      eventGuestAdded: "Attendee added.",
      eventGuestDeleted: "Attendee removed.",
      eventGuestNeedSelect: "Select at least one attendee to delete.",
      eventGuestNeedPick: "Select at least one user to add.",
      eventFileAttached: "Attachment added.",
      eventFileDeleted: "Attachment removed.",
      eventCheckinOk: "Checked in successfully.",
      eventCheckinDone: "You have already checked in.",
      eventCheckinHint: "You are signed in. Confirm attendance before checking in on site.",
      eventCheckinBtn: "Confirm attendance",
      eventCheckinBtnDone: "Checked in",
      eventRsvpOk: "Attendance confirmed.",
      eventRsvpDone: "Attendance confirmed. Check in when the event starts and you arrive.",
      eventRsvpWait: "Attendance confirmed. The event has not started — check-in opens at start time.",
      eventCheckinReady: "The event is in progress. Tap check-in to record your presence.",
      eventCheckinBtnGo: "Check in now",
      eventCheckinEnded: "The event has ended. Check-in is closed.",
      eventCheckinNeedLogin: "Sign in to confirm event attendance.",
      eventCheckinLoginOk: "Signed in successfully.",
      eventCheckinNeedUser: "Enter username and password.",
      eventCheckinKickerLogin: "Sign in",
      eventCheckinKickerRsvp: "Confirm attendance",
      eventCheckinKickerCheck: "Check-in",
      calFiltered: "Calendar filtered.",
      calReset: "Calendar reset.",
      openMenu: "Open menu",
      closeMenu: "Close menu",
      notifications: "Notifications",
      messages: "Messages",
      markAllRead: "Mark all as read",
      notifyEmpty: "No notifications.",
      viewNotify: "View all notifications",
      langVi: "Vietnamese",
      langEn: "English",
      notifyRead: "All notifications marked as read.",
      nSignTitle: "3 documents awaiting signature",
      nSignBody: "Due today · Signing queue",
      nChatTitle: "New message",
      nChatBody: "nguyen ho phi long just sent a message",
      nEventTitle: "Weekly standup",
      nEventBody: "Starts 09:00 on 11 Sep",
      nSocialTitle: "Post comment",
      nSocialBody: "Deputy head replied to you",
      nDocTitle: "New incoming document",
      nDocBody: "An internal document has been issued",
      pdfDownload: "Download PDF",
      pdfClose: "Close",
      pdfPage: "1 / 1"
    }
  };

  function t(key) {
    var lang = document.documentElement.lang === "en" ? "en" : "vi";
    return (i18n[lang] && i18n[lang][key]) || i18n.vi[key] || key;
  }

  function applyI18n() {
    document.querySelectorAll("[data-i18n]").forEach(function (el) {
      el.textContent = t(el.getAttribute("data-i18n"));
    });
    document.querySelectorAll("[data-i18n-aria]").forEach(function (el) {
      el.setAttribute("aria-label", t(el.getAttribute("data-i18n-aria")));
    });
  }

  function setLang(lang) {
    document.documentElement.lang = lang;
    try { localStorage.setItem("hcs-lang", lang); } catch (e) {}
    applyI18n();
    syncLangChip(lang);
    renderNotifyPanel();
  }

  var LANG_FLAGS = {
    vi: '<svg class="flag" viewBox="0 0 18 12" aria-hidden="true"><rect width="18" height="12" fill="#DA251D"/><polygon points="9,2 10.2,5.6 14,5.6 11,7.8 12.2,11.4 9,9.2 5.8,11.4 7,7.8 4,5.6 7.8,5.6" fill="#FFCD00"/></svg>',
    en: '<svg class="flag" viewBox="0 0 18 12" aria-hidden="true"><rect width="18" height="12" fill="#012169"/><path d="M0 0l18 12M18 0L0 12" stroke="#FFF" stroke-width="2.4"/><path d="M0 0l18 12M18 0L0 12" stroke="#C8102E" stroke-width="1.2"/><rect x="7.4" width="3.2" height="12" fill="#FFF"/><rect y="4.4" width="18" height="3.2" fill="#FFF"/><rect x="8" width="2" height="12" fill="#C8102E"/><rect y="5" width="18" height="2" fill="#C8102E"/></svg>'
  };

  function syncLangChip(lang) {
    var current = lang === "en" ? "en" : "vi";
    var slot = document.querySelector("[data-lang-flag]");
    if (slot) slot.innerHTML = LANG_FLAGS[current];
    var code = document.querySelector("[data-lang-code]");
    if (code) code.textContent = current === "en" ? "EN" : "VI";
    document.querySelectorAll("[data-set-lang]").forEach(function (btn) {
      var on = btn.getAttribute("data-set-lang") === current;
      btn.setAttribute("aria-checked", on ? "true" : "false");
    });
    var langBtn = document.getElementById("lang-btn");
    if (langBtn) langBtn.setAttribute("aria-label", t("chooseLang"));
  }

  function closeTopPops() {
    document.querySelectorAll(".top-pop").forEach(function (pop) {
      pop.classList.remove("is-open");
    });
    ["lang-btn", "notify-btn"].forEach(function (id) {
      var btn = document.getElementById(id);
      if (btn) btn.setAttribute("aria-expanded", "false");
    });
    ["lang-menu", "notify-panel"].forEach(function (id) {
      var el = document.getElementById(id);
      if (el) el.hidden = true;
    });
  }

  function initials(name) {
    var parts = String(name || "HU").trim().split(/\s+/);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }

  var PDF_FILE = "assets/demo-nghi-phep.pdf";

  function pdfPaperHtml(kind) {
    if (kind === "ksk") {
      return (
        '<p class="pdf-org">Bệnh viện đa khoa Bình Dương</p>' +
        '<p class="pdf-org-sub">Phòng Kế hoạch tổng hợp</p>' +
        '<div class="pdf-rule"></div>' +
        '<h2 class="pdf-doc-title">Công văn đồng bộ dữ liệu KSK</h2>' +
        '<p class="pdf-code">Số: 2062-WF / KHTH · Ngày 11/09/2026</p>' +
        "<p>Kính gửi: Phòng Công nghệ thông tin; các khoa/phòng liên quan.</p>" +
        "<p>Phòng KHTH đề nghị các đơn vị phối hợp đồng bộ dữ liệu khám sức khỏe định kỳ toàn dân theo lịch tuần 37/2026.</p>" +
        '<table class="pdf-meta">' +
        "<tr><th>Mã quy trình</th><td>2062-WF</td></tr>" +
        "<tr><th>Phạm vi</th><td>Toàn bệnh viện</td></tr>" +
        "<tr><th>Hạn hoàn thành</th><td>18/09/2026</td></tr>" +
        "<tr><th>Người theo dõi</th><td>Trần Việt Hùng</td></tr>" +
        "</table>" +
        "<p>Đơn vị nhận văn bản phản hồi kết quả đồng bộ qua hệ thống HCS trước 16:00 ngày hạn.</p>" +
        '<div class="pdf-sign"><div><strong>Người soạn</strong><p>Trần Việt Hùng</p></div><div><strong>Trưởng phòng KHTH</strong><p>Phó trưởng phòng</p></div></div>'
      );
    }
    if (kind === "qd") {
      return (
        '<p class="pdf-org">Bệnh viện đa khoa Bình Dương</p>' +
        '<p class="pdf-org-sub">Ban Giám đốc</p>' +
        '<div class="pdf-rule"></div>' +
        '<h2 class="pdf-doc-title">Quyết định</h2>' +
        '<p class="pdf-code">Số: 1551 / QĐ-BVBĐ · Ngày 23/08/2026</p>' +
        "<p>Về việc ban hành quy trình điện tử trên hệ thống Hành chính số.</p>" +
        '<table class="pdf-meta">' +
        "<tr><th>Căn cứ</th><td>Quy chế làm việc bệnh viện; quy trình QD-WF-09</td></tr>" +
        "<tr><th>Phạm vi áp dụng</th><td>Toàn bộ phòng ban</td></tr>" +
        "<tr><th>Hiệu lực</th><td>Từ ngày 01/09/2026</td></tr>" +
        "</table>" +
        "<p>Giao Phòng CNTT hướng dẫn triển khai; các trưởng khoa/phòng tổ chức thực hiện.</p>" +
        '<div class="pdf-sign"><div><strong>Nơi nhận</strong><p>Các khoa/phòng<br>Lưu VT</p></div><div><strong>Giám đốc</strong><p>Ban giám đốc</p></div></div>'
      );
    }
    if (kind === "minutes") {
      return (
        '<p class="pdf-org">Bệnh viện đa khoa Bình Dương</p>' +
        '<p class="pdf-org-sub">Phòng Hành chính quản trị</p>' +
        '<div class="pdf-rule"></div>' +
        '<h2 class="pdf-doc-title">Biên bản họp giao ban</h2>' +
        '<p class="pdf-code">File: Bien_ban_hop.pdf · Ngày 11/09/2026</p>' +
        "<p>Thời gian: 09:00 · Địa điểm: Phòng họp A · Chủ trì: Ban Giám đốc.</p>" +
        '<table class="pdf-meta">' +
        "<tr><th>Thành phần</th><td>BGĐ, KHTH, CNTT, HCQT</td></tr>" +
        "<tr><th>Nội dung</th><td>Tiến độ HCS, hàng đợi ký, sự kiện tuần</td></tr>" +
        "<tr><th>Kết luận</th><td>Duyệt mẫu đơn nghỉ phép điện tử trước 15/09</td></tr>" +
        "</table>" +
        '<div class="pdf-sign"><div><strong>Thư ký</strong><p>Trần Việt Hùng</p></div><div><strong>Chủ trì</strong><p>Ban giám đốc</p></div></div>'
      );
    }
    if (kind === "guide") {
      return (
        '<p class="pdf-org">Hệ thống Hành chính số — HCS</p>' +
        '<p class="pdf-org-sub">Hướng dẫn nội bộ</p>' +
        '<div class="pdf-rule"></div>' +
        '<h2 class="pdf-doc-title">Hướng dẫn trình ký văn bản điện tử</h2>' +
        '<p class="pdf-code">Huong_dan_trinh_ky.pdf · Cập nhật 09/09/2026</p>' +
        "<p>1. Tạo văn bản tại <strong>Văn bản của tôi</strong>, đính kèm file PDF.</p>" +
        "<p>2. Chọn quy trình và người ký tuần tự trên <strong>Quy trình</strong>.</p>" +
        "<p>3. Theo dõi trạng thái tại <strong>Hàng đợi ký</strong>.</p>" +
        "<p>4. Sau khi đủ chữ ký, tải bản PDF đã ban hành.</p>" +
        '<div class="pdf-sign"><div><strong>Người biên soạn</strong><p>Trần Việt Hùng</p></div><div><strong>Phê duyệt</strong><p>Phó trưởng phòng</p></div></div>'
      );
    }
    return (
      '<p class="pdf-org">Bệnh viện đa khoa Bình Dương</p>' +
      '<p class="pdf-org-sub">Phòng Kế hoạch tổng hợp</p>' +
      '<div class="pdf-rule"></div>' +
      '<h2 class="pdf-doc-title">Đơn xin nghỉ phép</h2>' +
      '<p class="pdf-code">Số: NP-tpl2 · Ngày 11/09/2026</p>' +
      "<p>Kính gửi: Ban Giám đốc.</p>" +
      "<p>Tôi là Trần Việt Hùng, chuyên viên Phòng Công nghệ thông tin, làm đơn xin nghỉ phép năm như sau:</p>" +
      '<table class="pdf-meta">' +
      "<tr><th>Họ và tên</th><td>Trần Việt Hùng</td></tr>" +
      "<tr><th>Đơn vị</th><td>Phòng Công nghệ thông tin</td></tr>" +
      "<tr><th>Lý do</th><td>Nghỉ phép năm 2026</td></tr>" +
      "<tr><th>Thời gian</th><td>15/09/2026 – 17/09/2026 (03 ngày)</td></tr>" +
      "<tr><th>Người bàn giao</th><td>Nguyễn Hồ Phi Long</td></tr>" +
      "</table>" +
      "<p>Tôi cam kết hoàn thành công việc bàn giao trước khi nghỉ và sẽ có mặt lại đúng hạn.</p>" +
      '<div class="pdf-sign"><div><strong>Ý kiến đơn vị</strong><p>Phó trưởng phòng</p></div><div><strong>Người làm đơn</strong><p>Trần Việt Hùng</p></div></div>'
    );
  }

  function pdfMetaFrom(el) {
    var key = (el && (el.getAttribute("data-pdf-open") || el.getAttribute("data-pdf-kind"))) || "leave";
    var map = {
      "np-tpl2": { title: "NP-tpl2 — Quy trình duyệt nghỉ phép", file: "Don_xin_nghi_phep.pdf", kind: "leave" },
      "np-tpl1": { title: "NP-tpl1 — Quy trình duyệt nghỉ phép", file: "Don_xin_nghi_phep.pdf", kind: "leave" },
      "np-tpl1b": { title: "NP-tpl1 — Quy trình duyệt nghỉ phép", file: "Don_xin_nghi_phep.pdf", kind: "leave" },
      "2062-wf": { title: "2062-WF — Đồng bộ dữ liệu KSK", file: "Dong_bo_KSK.pdf", kind: "ksk" },
      "qd-wf-09": { title: "QD-WF-09 — Quy trình ban hành quyết định", file: "Quyet_dinh_1551.pdf", kind: "qd" },
      "1551": { title: "QĐ 1551", file: "Quyet_dinh_1551.pdf", kind: "qd" },
      leave: { title: "Đơn xin nghỉ phép", file: "Don_xin_nghi_phep.pdf", kind: "leave" },
      minutes: { title: "Biên bản họp", file: "Bien_ban_hop.pdf", kind: "minutes" },
      guide: { title: "Hướng dẫn trình ký", file: "Huong_dan_trinh_ky.pdf", kind: "guide" }
    };
    var meta = map[key] || map.leave;
    var customTitle = el && el.getAttribute("data-pdf-title");
    var customFile = el && el.getAttribute("data-pdf-file");
    if (customTitle) meta = { title: customTitle, file: meta.file, kind: meta.kind };
    if (customFile) meta = { title: meta.title, file: customFile, kind: meta.kind };
    return meta;
  }

  function ensurePdfModal() {
    var overlay = document.getElementById("pdf-modal");
    if (overlay) return overlay;
    overlay = document.createElement("div");
    overlay.className = "modal-overlay";
    overlay.id = "pdf-modal";
    overlay.hidden = true;
    overlay.innerHTML =
      '<div class="pdf-dialog" role="dialog" aria-modal="true" aria-labelledby="pdf-modal-title" tabindex="-1">' +
        '<div class="pdf-toolbar">' +
          '<div class="pdf-toolbar-meta">' +
            '<strong id="pdf-modal-title">Tài liệu PDF</strong>' +
            '<span id="pdf-modal-file">demo.pdf</span>' +
          "</div>" +
          '<div class="pdf-toolbar-actions">' +
            '<span id="pdf-modal-page"></span>' +
            '<a class="btn btn-outline" id="pdf-download" href="' + PDF_FILE + '" download="Don_xin_nghi_phep.pdf">' +
              '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M12 4v10"/><path d="m8 10 4 4 4-4"/><path d="M5 20h14"/></svg>' +
              '<span class="btn-label" data-i18n="pdfDownload">Tải PDF</span>' +
            "</a>" +
            '<button class="btn btn-outline" type="button" data-pdf-close>' +
              '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M6 6l12 12M18 6 6 18"/></svg>' +
              '<span class="btn-label" data-i18n="pdfClose">Đóng</span>' +
            "</button>" +
          "</div>" +
        "</div>" +
        '<div class="pdf-stage"><article class="pdf-page" id="pdf-page"></article></div>' +
      "</div>";
    document.body.appendChild(overlay);
    overlay.addEventListener("click", function (event) {
      if (event.target === overlay || event.target.closest("[data-pdf-close]")) closePdfModal();
    });
    return overlay;
  }

  function openPdfModal(meta) {
    var overlay = ensurePdfModal();
    var info = meta || { title: "Đơn xin nghỉ phép", file: "Don_xin_nghi_phep.pdf", kind: "leave" };
    var title = document.getElementById("pdf-modal-title");
    var file = document.getElementById("pdf-modal-file");
    var page = document.getElementById("pdf-modal-page");
    var paper = document.getElementById("pdf-page");
    var download = document.getElementById("pdf-download");
    if (title) title.textContent = info.title;
    if (file) file.textContent = info.file + " · " + t("pdfPage");
    if (page) page.textContent = t("pdfPage");
    if (paper) paper.innerHTML = pdfPaperHtml(info.kind);
    if (download) {
      download.setAttribute("download", info.file);
      download.href = PDF_FILE;
      var dlLabel = download.querySelector("[data-i18n='pdfDownload']");
      if (dlLabel) dlLabel.textContent = t("pdfDownload");
    }
    var closeLabel = overlay.querySelector("[data-pdf-close] .btn-label");
    if (closeLabel) closeLabel.textContent = t("pdfClose");
    overlay.hidden = false;
    overlay.classList.add("is-open");
    document.body.classList.add("modal-open");
    var dialog = overlay.querySelector(".pdf-dialog");
    if (dialog) dialog.focus();
  }

  function closePdfModal() {
    var overlay = document.getElementById("pdf-modal");
    if (!overlay) return;
    overlay.classList.remove("is-open");
    overlay.hidden = true;
    document.body.classList.remove("modal-open");
  }

  function showToast(message) {
    var toast = document.getElementById("toast");
    if (!toast) return;
    toast.textContent = message;
    toast.classList.add("is-open");
    window.clearTimeout(showToast._timer);
    showToast._timer = window.setTimeout(function () {
      toast.classList.remove("is-open");
    }, 2400);
  }

  function bindSearchToggles() {
    document.querySelectorAll("[data-search-toggle]").forEach(function (toggle) {
      var form = document.getElementById(toggle.getAttribute("data-search-toggle"));
      if (!form) return;
      var focusId = toggle.getAttribute("data-search-focus");
      function hasValue() {
        var filled = false;
        form.querySelectorAll("input[type='search'], input[name='q']").forEach(function (field) {
          if ((field.value || "").trim()) filled = true;
        });
        return filled;
      }
      function sync() {
        var open = !form.hidden;
        toggle.setAttribute("aria-expanded", open ? "true" : "false");
        toggle.classList.toggle("is-open", open);
        toggle.classList.toggle("is-filtered", hasValue());
      }
      toggle.addEventListener("click", function () {
        form.hidden = !form.hidden;
        sync();
        if (!form.hidden && focusId) {
          var el = document.getElementById(focusId);
          if (el) el.focus();
        }
      });
      form.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
          event.preventDefault();
          form.hidden = true;
          sync();
          toggle.focus();
        }
      });
      form.addEventListener("input", sync);
      form.addEventListener("change", sync);
      form.addEventListener("submit", function () {
        window.setTimeout(sync, 0);
      });
      sync();
    });
  }

  bindSearchToggles();

  function redirectHome(username) {
    try {
      sessionStorage.setItem("hcs-user", username || "hungtv");
    } catch (e) {}
    window.location.href = "home.html";
  }

  var savedLang = "vi";
  try { savedLang = localStorage.getItem("hcs-lang") || "vi"; } catch (e) {}
  document.documentElement.lang = savedLang;
  applyI18n();
  syncLangChip(savedLang);

  document.querySelectorAll("[data-lang]").forEach(function (el) {
    el.addEventListener("change", function () {
      setLang(el.value);
    });
    el.addEventListener("click", function () {
      if (el.dataset.lang) setLang(el.dataset.lang);
    });
    if (el.tagName === "SELECT") el.value = savedLang;
  });

  var NOTIFY_ICONS = {
    sign: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z"/></svg>',
    chat: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 6h16v10H7l-3 3V6z"/></svg>',
    event: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/></svg>',
    social: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 6h16v10H7l-3 3V6z"/></svg>',
    doc: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M7 3h7l5 5v13H7z"/><path d="M14 3v5h5"/></svg>'
  };
  var NOTIFY_ITEMS = [
    { id: "sign", href: "ky-duyet.html", icon: "sign", title: "nSignTitle", body: "nSignBody", time: "10 phút trước" },
    { id: "chat", href: "chat.html", icon: "chat", title: "nChatTitle", body: "nChatBody", time: "25 phút trước" },
    { id: "event", href: "su-kien.html", icon: "event", title: "nEventTitle", body: "nEventBody", time: "1 giờ trước" },
    { id: "social", href: "bang-tin.html", icon: "social", title: "nSocialTitle", body: "nSocialBody", time: "Hôm qua" },
    { id: "doc", href: "van-ban.html", icon: "doc", title: "nDocTitle", body: "nDocBody", time: "08/09" }
  ];

  function readNotifyState() {
    try {
      return JSON.parse(sessionStorage.getItem("hcs-notify-read") || "[]");
    } catch (e) {
      return [];
    }
  }

  function writeNotifyState(ids) {
    try { sessionStorage.setItem("hcs-notify-read", JSON.stringify(ids)); } catch (e) {}
  }

  function unreadNotifyIds() {
    var read = readNotifyState();
    return NOTIFY_ITEMS.filter(function (item) { return read.indexOf(item.id) === -1; }).map(function (item) { return item.id; });
  }

  function syncNotifyBadge() {
    var count = unreadNotifyIds().length;
    document.querySelectorAll("[data-notify-count]").forEach(function (badge) {
      badge.textContent = String(count);
      badge.hidden = count === 0;
    });
  }

  function renderNotifyPanel() {
    var panel = document.getElementById("notify-panel");
    if (!panel) return;
    var read = readNotifyState();
    var list = NOTIFY_ITEMS.map(function (item) {
      var unread = read.indexOf(item.id) === -1;
      return '<a class="notify-item' + (unread ? " is-unread" : "") + '" href="' + item.href + '" data-notify-id="' + item.id + '">' +
        '<span class="notify-ico is-' + item.icon + '" aria-hidden="true">' + NOTIFY_ICONS[item.icon] + "</span>" +
        '<span class="notify-copy"><strong>' + t(item.title) + "</strong>" +
        "<p>" + t(item.body) + "</p><time>" + item.time + "</time></span></a>";
    }).join("");
    var box = panel.querySelector("[data-notify-list]");
    if (box) box.innerHTML = list || '<p class="notify-empty">' + t("notifyEmpty") + "</p>";
    var mark = panel.querySelector("[data-notify-read]");
    if (mark) mark.textContent = t("markAllRead");
    var title = panel.querySelector("#notify-title");
    if (title) title.textContent = t("notifications");
    var all = panel.querySelector("[data-i18n='viewNotify']");
    if (all) all.textContent = t("viewNotify");
    syncNotifyBadge();
  }

  function initTopChrome() {
    var langPop = document.getElementById("lang-pop");
    var langBtn = document.getElementById("lang-btn");
    var notifyPop = document.getElementById("notify-pop");
    var notifyBtn = document.getElementById("notify-btn");
    if (langPop && langBtn && !document.getElementById("lang-menu")) {
      var menu = document.createElement("div");
      menu.id = "lang-menu";
      menu.className = "top-menu";
      menu.hidden = true;
      menu.setAttribute("role", "menu");
      menu.innerHTML =
        '<button type="button" role="menuitemradio" data-set-lang="vi">' +
        '<svg class="flag" viewBox="0 0 18 12" aria-hidden="true"><rect width="18" height="12" fill="#DA251D"/><polygon points="9,2 10.2,5.6 14,5.6 11,7.8 12.2,11.4 9,9.2 5.8,11.4 7,7.8 4,5.6 7.8,5.6" fill="#FFCD00"/></svg>' +
        '<span data-i18n="langVi">' + t("langVi") + "</span>" +
        '<svg class="top-check" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true"><path d="M5 12.5 9.5 17 19 7"/></svg></button>' +
        '<button type="button" role="menuitemradio" data-set-lang="en">' +
        '<svg class="flag" viewBox="0 0 18 12" aria-hidden="true"><rect width="18" height="12" fill="#012169"/><path d="M0 0l18 12M18 0L0 12" stroke="#FFF" stroke-width="2.4"/><path d="M0 0l18 12M18 0L0 12" stroke="#C8102E" stroke-width="1.2"/><rect x="7.4" width="3.2" height="12" fill="#FFF"/><rect y="4.4" width="18" height="3.2" fill="#FFF"/><rect x="8" width="2" height="12" fill="#C8102E"/><rect y="5" width="18" height="2" fill="#C8102E"/></svg>' +
        '<span data-i18n="langEn">' + t("langEn") + "</span>" +
        '<svg class="top-check" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true"><path d="M5 12.5 9.5 17 19 7"/></svg></button>';
      langPop.appendChild(menu);
    }
    if (notifyPop && notifyBtn && !document.getElementById("notify-panel")) {
      var panel = document.createElement("div");
      panel.id = "notify-panel";
      panel.className = "top-panel";
      panel.hidden = true;
      panel.setAttribute("role", "region");
      panel.setAttribute("aria-labelledby", "notify-title");
      panel.innerHTML =
        '<div class="top-panel-head"><h2 id="notify-title">' + t("notifications") + "</h2>" +
        '<button type="button" data-notify-read>' + t("markAllRead") + "</button></div>" +
        '<div class="notify-list" data-notify-list></div>' +
        '<div class="top-panel-foot">' +
        '<a href="home.html" data-i18n="viewNotify">' + t("viewNotify") + "</a></div>";
      notifyPop.appendChild(panel);
    }
    renderNotifyPanel();
    syncLangChip(document.documentElement.lang === "en" ? "en" : "vi");

    function togglePop(pop, btn, panel) {
      if (!pop || !btn || !panel) return;
      var willOpen = panel.hidden;
      closeTopPops();
      if (willOpen) {
        pop.classList.add("is-open");
        panel.hidden = false;
        btn.setAttribute("aria-expanded", "true");
      }
    }

    if (langBtn) {
      langBtn.addEventListener("click", function (event) {
        event.stopPropagation();
        togglePop(langPop, langBtn, document.getElementById("lang-menu"));
      });
    }
    if (notifyBtn) {
      notifyBtn.addEventListener("click", function (event) {
        event.stopPropagation();
        togglePop(notifyPop, notifyBtn, document.getElementById("notify-panel"));
      });
    }
    if (langPop) {
      langPop.addEventListener("click", function (event) {
        var pick = event.target.closest("[data-set-lang]");
        if (!pick) return;
        setLang(pick.getAttribute("data-set-lang"));
        closeTopPops();
      });
    }
    if (notifyPop) {
      notifyPop.addEventListener("click", function (event) {
        var mark = event.target.closest("[data-notify-read]");
        if (mark) {
          writeNotifyState(NOTIFY_ITEMS.map(function (item) { return item.id; }));
          renderNotifyPanel();
          showToast(t("notifyRead"));
          return;
        }
        var item = event.target.closest("[data-notify-id]");
        if (item) {
          var ids = readNotifyState();
          var id = item.getAttribute("data-notify-id");
          if (ids.indexOf(id) === -1) {
            ids.push(id);
            writeNotifyState(ids);
          }
        }
      });
    }
  }

  initTopChrome();

  var toggle = document.querySelector("[data-toggle-password]");
  if (toggle) {
    toggle.addEventListener("click", function () {
      var input = document.getElementById("password");
      if (!input) return;
      var hidden = input.type === "password";
      input.type = hidden ? "text" : "password";
      toggle.setAttribute("aria-label", hidden ? t("hidePassword") : t("showPassword"));
      toggle.setAttribute("aria-pressed", hidden ? "true" : "false");
    });
  }

  var loginForm = document.getElementById("login-form");
  if (loginForm) {
    loginForm.addEventListener("submit", function (event) {
      event.preventDefault();
      var user = document.getElementById("username");
      var pass = document.getElementById("password");
      var userErr = document.getElementById("username-error");
      var passErr = document.getElementById("password-error");
      var valid = true;

      user.classList.remove("is-invalid");
      pass.classList.remove("is-invalid");
      userErr.classList.remove("is-visible");
      passErr.classList.remove("is-visible");

      if (!user.value.trim()) {
        user.classList.add("is-invalid");
        userErr.textContent = t("userRequired");
        userErr.classList.add("is-visible");
        valid = false;
      }
      if (!pass.value) {
        pass.classList.add("is-invalid");
        passErr.textContent = t("passRequired");
        passErr.classList.add("is-visible");
        valid = false;
      }
      if (!valid) {
        (user.value.trim() ? pass : user).focus();
        return;
      }

      var submit = loginForm.querySelector("[type='submit']");
      submit.disabled = true;
      submit.setAttribute("aria-busy", "true");
      var icon = submit.querySelector("svg");
      submit.innerHTML = (icon ? icon.outerHTML : "") + '<span class="spinner" aria-hidden="true"></span><span data-i18n="login">' + t("login") + "</span>";
      window.setTimeout(function () {
        redirectHome(user.value.trim());
      }, 500);
    });
  }

  var ssoBtn = document.getElementById("sso-btn");
  var ssoOverlay = document.getElementById("sso-overlay");
  if (ssoBtn && ssoOverlay) {
    ssoBtn.addEventListener("click", function () {
      ssoOverlay.classList.add("is-open");
      ssoBtn.disabled = true;
      ssoBtn.setAttribute("aria-busy", "true");
      window.setTimeout(function () {
        redirectHome("hungtv");
      }, 900);
    });
  }

  var menuToggle = document.getElementById("menu-toggle");
  var nav = document.getElementById("app-nav");
  var overlay = document.getElementById("nav-overlay");

  function setNavOpen(open) {
    if (!nav || !menuToggle) return;
    nav.classList.toggle("is-open", open);
    menuToggle.setAttribute("aria-expanded", open ? "true" : "false");
    menuToggle.setAttribute("aria-label", open ? t("closeMenu") : t("openMenu"));
    document.body.classList.toggle("nav-open", open);
    if (overlay) {
      overlay.classList.toggle("is-open", open);
      overlay.hidden = !open;
    }
  }

  if (menuToggle && nav) {
    menuToggle.addEventListener("click", function () {
      setNavOpen(!nav.classList.contains("is-open"));
    });
  }

  if (overlay) {
    overlay.addEventListener("click", function () {
      setNavOpen(false);
    });
  }

  if (location.hash === "#menu") {
    setNavOpen(true);
  }

  if (location.hash === "#danhmuc") {
    document.querySelectorAll("[data-nav-menu]").forEach(function (btn) {
      if ((btn.textContent || "").indexOf("Danh mục") !== -1) {
        btn.setAttribute("aria-expanded", "true");
        var menu = btn.parentElement.querySelector(".menu");
        if (menu) menu.classList.add("is-open");
      }
    });
  }

  var processModal = document.getElementById("process-modal");
  var processModalBody = document.getElementById("process-modal-body");
  var processLastFocus = null;

  function getFocusable(root) {
    if (!root) return [];
    return Array.prototype.slice.call(
      root.querySelectorAll('button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])')
    ).filter(function (el) {
      return !el.hasAttribute("hidden") && el.offsetParent !== null;
    });
  }

  function closeProcessModal() {
    if (!processModal || !processModal.classList.contains("is-open")) return;
    processModal.classList.remove("is-open");
    processModal.hidden = true;
    document.body.classList.remove("modal-open");
    if (processLastFocus && typeof processLastFocus.focus === "function") {
      processLastFocus.focus();
    }
    processLastFocus = null;
  }

  function setProcessPane(name) {
    if (!processModalBody) return;
    processModalBody.querySelectorAll("[data-process-tab]").forEach(function (tab) {
      var on = tab.getAttribute("data-process-tab") === name;
      tab.classList.toggle("is-active", on);
      tab.setAttribute("aria-selected", on ? "true" : "false");
    });
    processModalBody.querySelectorAll("[data-pane]").forEach(function (pane) {
      pane.hidden = pane.getAttribute("data-pane") !== name;
    });
  }

  function openProcessModal(id, trigger) {
    var tpl = document.getElementById(id);
    if (!processModal || !processModalBody || !tpl) return;
    processLastFocus = trigger || document.activeElement;
    processModalBody.innerHTML = "";
    processModalBody.appendChild(tpl.content.cloneNode(true));
    processModal.hidden = false;
    processModal.classList.add("is-open");
    document.body.classList.add("modal-open");
    setNavOpen(false);
    var dialog = processModal.querySelector(".process-dialog");
    var closeBtn = processModal.querySelector("[data-close-modal]");
    (closeBtn || dialog).focus();
  }

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
      var pdfModalEsc = document.getElementById("pdf-modal");
      if (pdfModalEsc && pdfModalEsc.classList.contains("is-open")) {
        closePdfModal();
        return;
      }
      var signModalEsc = document.getElementById("sign-modal");
      if (signModalEsc && signModalEsc.classList.contains("is-open")) {
        signModalEsc.classList.remove("is-open");
        signModalEsc.hidden = true;
        document.body.classList.remove("modal-open");
        return;
      }
      if (processModal && processModal.classList.contains("is-open")) {
        closeProcessModal();
        return;
      }
      var wfStepModalEsc = document.getElementById("wf-step-modal");
      if (wfStepModalEsc && wfStepModalEsc.classList.contains("is-open")) {
        wfStepModalEsc.classList.remove("is-open");
        wfStepModalEsc.hidden = true;
        document.body.classList.remove("modal-open");
        return;
      }
      var dmModalEsc = document.getElementById("dm-modal");
      if (dmModalEsc && dmModalEsc.classList.contains("is-open")) {
        dmModalEsc.classList.remove("is-open");
        dmModalEsc.hidden = true;
        document.body.classList.remove("modal-open");
        return;
      }
      var chatShellEsc = document.getElementById("chat-shell");
      var msgMenuEsc = document.getElementById("msg-menu");
      var chatDlgEsc = document.getElementById("chat-dialog");
      if (msgMenuEsc && !msgMenuEsc.hidden) {
        msgMenuEsc.hidden = true;
        document.querySelectorAll(".msg-more[aria-expanded='true']").forEach(function (btn) {
          btn.setAttribute("aria-expanded", "false");
        });
        return;
      }
      if (chatDlgEsc && chatDlgEsc.classList.contains("is-open")) {
        chatDlgEsc.classList.remove("is-open");
        chatDlgEsc.hidden = true;
        document.body.classList.remove("modal-open");
        return;
      }
      var openTop = document.querySelector(".top-pop.is-open");
      if (openTop) {
        closeTopPops();
        return;
      }
      var openReact = document.querySelector(".react-wrap.is-open");
      if (openReact) {
        openReact.classList.remove("is-open");
        return;
      }
      if (chatShellEsc && chatShellEsc.classList.contains("is-info-open")) {
        chatShellEsc.classList.remove("is-info-open");
        var infoEsc = document.getElementById("chat-info");
        if (infoEsc) infoEsc.hidden = true;
        var moreEsc = document.getElementById("chat-more-btn");
        if (moreEsc) {
          moreEsc.classList.remove("is-active");
          moreEsc.setAttribute("aria-expanded", "false");
        }
        return;
      }
      setNavOpen(false);
      document.querySelectorAll("[data-nav-menu]").forEach(function (btn) {
        btn.setAttribute("aria-expanded", "false");
        var menu = btn.parentElement.querySelector(".menu");
        if (menu) menu.classList.remove("is-open");
      });
      var filterMenu = document.getElementById("filter-menu");
      if (filterMenu) filterMenu.classList.remove("is-open");
    }

    if (event.key === "Tab" && processModal && processModal.classList.contains("is-open")) {
      var dialog = processModal.querySelector(".process-dialog");
      var nodes = getFocusable(dialog);
      if (!nodes.length) return;
      var first = nodes[0];
      var last = nodes[nodes.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    }
  });

  if (processModal) {
    processModal.addEventListener("click", function (event) {
      if (event.target === processModal || event.target.closest("[data-close-modal]")) {
        closeProcessModal();
        return;
      }
      var tab = event.target.closest("[data-process-tab]");
      if (tab) {
        setProcessPane(tab.getAttribute("data-process-tab"));
      }
    });
  }

  document.querySelectorAll("[data-open-process]").forEach(function (el) {
    el.addEventListener("click", function (event) {
      event.preventDefault();
      openProcessModal(el.getAttribute("data-open-process"), el);
    });
  });

  var queueBody = document.getElementById("queue-body");
  if (queueBody) {
    var activeQueue = "all";
    var queueQuery = "";
    var queuePresenter = "";

    function applyQueueFilter() {
      var rows = queueBody.querySelectorAll("tr");
      var visible = 0;
      rows.forEach(function (row) {
        var queue = row.getAttribute("data-queue") || "";
        var text = (row.getAttribute("data-text") || "").toLowerCase();
        var queueOk = activeQueue === "all" || queue === activeQueue;
        var textOk = !queueQuery || text.indexOf(queueQuery) !== -1;
        var presenterOk = !queuePresenter || text.indexOf(queuePresenter) !== -1;
        var show = queueOk && textOk && presenterOk;
        row.hidden = !show;
        if (show) visible += 1;
      });
      var empty = document.getElementById("queue-empty");
      if (empty) empty.hidden = visible > 0;
      return visible;
    }

    document.querySelectorAll(".queue-tab").forEach(function (btn) {
      btn.addEventListener("click", function () {
        document.querySelectorAll(".queue-tab").forEach(function (el) {
          var on = el === btn;
          el.classList.toggle("is-active", on);
          el.setAttribute("aria-selected", on ? "true" : "false");
        });
        activeQueue = btn.getAttribute("data-queue") || "all";
        applyQueueFilter();
      });
    });

    var queueForm = document.getElementById("queue-search-form");
    if (queueForm) {
      queueForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var input = document.getElementById("queue-q");
        var presenter = document.getElementById("queue-presenter");
        queueQuery = ((input && input.value) || "").trim().toLowerCase();
        queuePresenter = ((presenter && presenter.value) || "").trim().toLowerCase();
        applyQueueFilter();
        showToast(t("queueFiltered"));
      });
    }

    var refreshQueue = document.getElementById("refresh-queue");
    if (refreshQueue) {
      refreshQueue.addEventListener("click", function () {
        var input = document.getElementById("queue-q");
        var presenter = document.getElementById("queue-presenter");
        var dept = document.getElementById("queue-dept");
        var range = document.getElementById("queue-range");
        var dateType = document.getElementById("queue-date-type");
        if (input) input.value = "";
        if (presenter) presenter.selectedIndex = 0;
        if (dept) dept.selectedIndex = 0;
        if (range) range.value = "";
        if (dateType) dateType.selectedIndex = 0;
        queueQuery = "";
        queuePresenter = "";
        activeQueue = "all";
        document.querySelectorAll(".queue-tab").forEach(function (el) {
          var on = el.getAttribute("data-queue") === "all";
          el.classList.toggle("is-active", on);
          el.setAttribute("aria-selected", on ? "true" : "false");
        });
        applyQueueFilter();
        showToast(t("queueRefreshed"));
      });
    }
  }

  document.querySelectorAll("[data-nav-menu]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var expanded = btn.getAttribute("aria-expanded") === "true";
      document.querySelectorAll("[data-nav-menu]").forEach(function (other) {
        other.setAttribute("aria-expanded", "false");
        var menu = other.parentElement.querySelector(".menu");
        if (menu) menu.classList.remove("is-open");
      });
      btn.setAttribute("aria-expanded", expanded ? "false" : "true");
      var menu = btn.parentElement.querySelector(".menu");
      if (menu && !expanded) menu.classList.add("is-open");
    });
  });

  document.addEventListener("click", function (event) {
    if (event.target.closest(".menu-wrap")) return;
    document.querySelectorAll("[data-nav-menu]").forEach(function (btn) {
      btn.setAttribute("aria-expanded", "false");
      var menu = btn.parentElement.querySelector(".menu");
      if (menu) menu.classList.remove("is-open");
    });
    if (!event.target.closest(".filter-pop")) {
      var filterMenu = document.getElementById("filter-menu");
      var filterBtn = document.getElementById("filter-btn");
      if (filterMenu) filterMenu.classList.remove("is-open");
      if (filterBtn) filterBtn.setAttribute("aria-expanded", "false");
    }
    if (!event.target.closest(".chat-more")) {
      var chatMore = document.getElementById("chat-more-menu");
      var chatMoreBtn = document.getElementById("chat-more-btn");
      if (chatMore) chatMore.hidden = true;
      if (chatMoreBtn) chatMoreBtn.setAttribute("aria-expanded", "false");
    }
    if (!event.target.closest("#msg-menu") && !event.target.closest(".msg-more") && !event.target.closest(".chat-thumb")) {
      var msgMenuClick = document.getElementById("msg-menu");
      if (msgMenuClick && !msgMenuClick.hidden) {
        msgMenuClick.hidden = true;
        document.querySelectorAll(".msg-more[aria-expanded='true']").forEach(function (btn) {
          btn.setAttribute("aria-expanded", "false");
        });
      }
    }
    if (!event.target.closest(".react-wrap")) {
      document.querySelectorAll(".react-wrap.is-open").forEach(function (wrap) {
        wrap.classList.remove("is-open");
      });
    }
    if (!event.target.closest(".top-pop")) {
      closeTopPops();
    }
  });

  var searchForm = document.getElementById("search-form");
  var wsWeek = document.getElementById("ws-week");
  if (searchForm && !wsWeek) {
    searchForm.addEventListener("submit", function (event) {
      event.preventDefault();
      showToast(t("searchDone"));
    });
  }

  (function initWorkspace() {
    var week = document.getElementById("ws-week");
    var list = document.getElementById("ws-events");
    var form = document.getElementById("search-form");
    if (!week || !list || !form) return;

    var from = document.getElementById("date-from");
    var to = document.getElementById("date-to");
    var empty = document.getElementById("ws-events-empty");
    var countEl = document.getElementById("ws-event-count");
    var kpiEl = document.getElementById("kpi-events");
    var badgeEl = document.getElementById("kpi-event-badge");
    var items = list.querySelectorAll(".cal-event");

    function selectedDay() {
      var btn = week.querySelector(".week-cell.is-selected");
      return btn ? btn.getAttribute("data-date") : "";
    }

    function inRange(date, start, end) {
      if (start && date < start) return false;
      if (end && date > end) return false;
      return true;
    }

    function applyFilter() {
      var start = from ? from.value : "";
      var end = to ? to.value : "";
      if (start && end && start > end) {
        var swap = start;
        start = end;
        end = swap;
        if (from) from.value = start;
        if (to) to.value = end;
      }
      var day = selectedDay();
      var visible = 0;
      var inRangeCount = 0;
      items.forEach(function (item) {
        var date = item.getAttribute("data-date") || "";
        var ranged = inRange(date, start, end);
        if (ranged) inRangeCount += 1;
        var show = ranged && (!day || date === day);
        item.hidden = !show;
        if (show) visible += 1;
      });
      if (empty) empty.hidden = visible > 0;
      if (countEl) countEl.textContent = "Tổng cộng " + inRangeCount + " sự kiện";
      if (kpiEl) kpiEl.textContent = String(inRangeCount);
      if (badgeEl) {
        badgeEl.textContent = String(inRangeCount);
        badgeEl.hidden = inRangeCount < 1;
      }
      week.querySelectorAll(".week-cell").forEach(function (cell) {
        var date = cell.getAttribute("data-date") || "";
        var has = false;
        items.forEach(function (item) {
          if ((item.getAttribute("data-date") || "") === date && inRange(date, start, end)) has = true;
        });
        cell.classList.toggle("has-event", has);
      });
    }

    week.addEventListener("click", function (event) {
      var cell = event.target.closest(".week-cell");
      if (!cell || !week.contains(cell)) return;
      week.querySelectorAll(".week-cell").forEach(function (btn) {
        btn.classList.toggle("is-selected", btn === cell);
        btn.setAttribute("aria-selected", btn === cell ? "true" : "false");
      });
      applyFilter();
    });

    form.addEventListener("submit", function (event) {
      event.preventDefault();
      applyFilter();
      showToast(t("searchDone"));
    });

    applyFilter();
  })();

  var userName = document.getElementById("user-name");
  var userAvatar = document.getElementById("user-avatar");
  if (userName || userAvatar) {
    var stored = "hungtv";
    try { stored = sessionStorage.getItem("hcs-user") || "hungtv"; } catch (e) {}
    if (userName) userName.textContent = stored;
    if (userAvatar) userAvatar.textContent = initials(stored);
  }

  document.querySelectorAll("[data-mock-action]").forEach(function (el) {
    el.addEventListener("click", function (event) {
      event.preventDefault();
      showToast(el.getAttribute("data-mock-action"));
    });
  });

  document.addEventListener("click", function (event) {
    var trigger = event.target.closest("[data-pdf-open]");
    if (!trigger) return;
    event.preventDefault();
    openPdfModal(pdfMetaFrom(trigger));
  });

  if (location.hash === "#pdf") {
    openPdfModal(pdfMetaFrom(null));
  }

  var SIGN_DEFAULT_DOC = "Văn bản: 2062 · Đồng bộ dữ liệu KSK định kỳ toàn dân";
  var signModal = document.getElementById("sign-modal");
  if (signModal) {
    function closeSignModal() {
      signModal.classList.remove("is-open");
      signModal.hidden = true;
      document.body.classList.remove("modal-open");
    }

    function openSignModal(label) {
      var doc = document.getElementById("sign-doc");
      var form = document.getElementById("sign-form");
      var err = document.getElementById("sign-wf-err");
      var wf = document.getElementById("sign-wf");
      var fileName = document.getElementById("sign-file-name");
      if (doc) doc.textContent = label || SIGN_DEFAULT_DOC;
      if (form) form.reset();
      if (err) err.classList.remove("is-visible");
      if (wf) wf.classList.remove("is-invalid");
      if (fileName) fileName.textContent = "No file chosen";
      signModal.hidden = false;
      signModal.classList.add("is-open");
      document.body.classList.add("modal-open");
      var dialog = signModal.querySelector(".sign-dialog");
      if (wf) wf.focus();
      else if (dialog) dialog.focus();
    }

    document.addEventListener("click", function (event) {
      var trigger = event.target.closest("[data-sign-open]");
      if (!trigger || trigger.disabled) return;
      event.preventDefault();
      var label = trigger.getAttribute("data-sign-doc");
      if (!label && document.getElementById("vb-editor") && !document.getElementById("vb-editor").hidden) {
        var code = ((document.getElementById("vb-code") || {}).value || "—").trim() || "—";
        var title = ((document.getElementById("vb-title") || {}).value || "Văn bản mới").trim() || "Văn bản mới";
        label = "Văn bản: " + code + " · " + title;
      }
      openSignModal(label);
    });

    signModal.addEventListener("click", function (event) {
      if (event.target === signModal || event.target.closest("[data-sign-close]")) closeSignModal();
    });

    var signFile = document.getElementById("sign-file");
    if (signFile) {
      signFile.addEventListener("change", function () {
        var names = Array.prototype.map.call(signFile.files || [], function (file) { return file.name; });
        var slot = document.getElementById("sign-file-name");
        if (slot) slot.textContent = names.length ? names.join(", ") : "No file chosen";
      });
    }

    var signForm = document.getElementById("sign-form");
    if (signForm) {
      signForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var wf = document.getElementById("sign-wf");
        var err = document.getElementById("sign-wf-err");
        if (!wf || !wf.value) {
          if (wf) {
            wf.classList.add("is-invalid");
            wf.focus();
          }
          if (err) err.classList.add("is-visible");
          showToast(t("signNeedWf"));
          return;
        }
        closeSignModal();
        showToast(t("signSubmitted"));
        window.setTimeout(function () {
          location.href = "ky-duyet.html";
        }, 650);
      });
    }

    if (location.hash === "#trinh-ky") openSignModal(SIGN_DEFAULT_DOC);
  }

  var docsBody = document.getElementById("docs-body");
  if (docsBody) {
    var activeCat = "all";
    var query = "";

    function applyDocsFilter() {
      var rows = docsBody.querySelectorAll("tr");
      var visible = 0;
      rows.forEach(function (row) {
        var cats = (row.getAttribute("data-cat") || "").split(/\s+/);
        var text = (row.getAttribute("data-text") || "").toLowerCase();
        var catOk = activeCat === "all" || cats.indexOf(activeCat) !== -1;
        var textOk = !query || text.indexOf(query) !== -1;
        var show = catOk && textOk;
        row.hidden = !show;
        if (show) visible += 1;
      });
      return visible;
    }

    document.querySelectorAll(".docs-cat").forEach(function (btn) {
      btn.addEventListener("click", function () {
        document.querySelectorAll(".docs-cat").forEach(function (el) {
          el.classList.remove("is-active");
        });
        btn.classList.add("is-active");
        activeCat = btn.getAttribute("data-cat") || "all";
        applyDocsFilter();
      });
    });

    var docsForm = document.getElementById("docs-search-form");
    if (docsForm) {
      docsForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var input = document.getElementById("docs-q");
        query = ((input && input.value) || "").trim().toLowerCase();
        applyDocsFilter();
        showToast(t("docsFiltered"));
      });
    }

    var refresh = document.getElementById("refresh-docs");
    if (refresh) {
      refresh.addEventListener("click", function () {
        var input = document.getElementById("docs-q");
        if (input) input.value = "";
        query = "";
        activeCat = "all";
        document.querySelectorAll(".docs-cat").forEach(function (el) {
          el.classList.toggle("is-active", el.getAttribute("data-cat") === "all");
        });
        applyDocsFilter();
        showToast(t("docsRefreshed"));
      });
    }

    var filterBtn = document.getElementById("filter-btn");
    var filterMenu = document.getElementById("filter-menu");
    if (filterBtn && filterMenu) {
      filterBtn.addEventListener("click", function (event) {
        event.stopPropagation();
        var open = !filterMenu.classList.contains("is-open");
        filterMenu.classList.toggle("is-open", open);
        filterBtn.setAttribute("aria-expanded", open ? "true" : "false");
      });
    }

    var vbList = document.getElementById("vb-list");
    var vbEditor = document.getElementById("vb-editor");
    var vbAssignees = [];
    var vbHistory = [];
    var vbPresented = false;
    var vbSaved = false;
    var vbDirty = false;
    var vbFilling = false;
    function vbEsc(value) {
      return String(value == null ? "" : value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
    }
    var VB_DOCS = {
      np: {
        code: "NP",
        title: "Đơn xin nghỉ phép",
        desc: "",
        type: "qt",
        field: "hc",
        urgency: "4",
        secret: "1",
        pdf: "leave",
        history: [
          { at: "23/08/2026 17:36", action: "Classified", note: "Đã phân loại" },
          { at: "23/08/2026 17:36", action: "Created", note: "Đã tạo" }
        ]
      },
      "1551": {
        code: "1551",
        title: "QĐ 1551",
        desc: "",
        type: "qd",
        field: "hc",
        urgency: "4",
        secret: "1",
        pdf: "qd",
        history: [
          { at: "23/08/2026 17:36", action: "Created", note: "Đã tạo" }
        ]
      }
    };

    function nowStamp() {
      return "11/09/2026 09:22";
    }

    function setVbCount(id, value) {
      var el = document.getElementById(id);
      if (!el) return;
      el.textContent = String(value);
      el.hidden = !value;
    }

    function showVbTab(name) {
      document.querySelectorAll("[data-vb-tab]").forEach(function (tab) {
        var on = tab.getAttribute("data-vb-tab") === name;
        tab.classList.toggle("is-active", on);
        tab.setAttribute("aria-selected", on ? "true" : "false");
      });
      document.querySelectorAll("[data-vb-pane]").forEach(function (pane) {
        var on = pane.getAttribute("data-vb-pane") === name;
        pane.classList.toggle("is-active", on);
        pane.hidden = !on;
      });
    }

    function renderVbAssignees() {
      var list = document.getElementById("vb-assignees");
      var empty = document.getElementById("vb-empty-data");
      if (!list) return;
      list.innerHTML = vbAssignees.map(function (item, index) {
        return '<li class="vb-chip">' +
          vbEsc(item.person) + " · " + vbEsc(item.role) +
          '<button type="button" data-unassign="' + index + '" aria-label="Bỏ phân công ' + vbEsc(item.person) + '">' +
          '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M6 6l12 12M18 6 6 18"/></svg>' +
          "</button></li>";
      }).join("");
      if (empty) empty.hidden = vbAssignees.length > 0;
      setVbCount("vb-assign-count", vbAssignees.length);
    }

    function renderVbHistory() {
      var list = document.getElementById("vb-timeline");
      if (!list) return;
      if (!vbHistory.length) {
        list.innerHTML = '<li class="vb-timeline-empty">Chưa có lịch sử. Các thao tác lưu, phân công, trình ký sẽ hiện tại đây.</li>';
      } else {
        list.innerHTML = vbHistory.map(function (item) {
          return '<li class="vb-timeline-item"><time>' + vbEsc(item.at) + "</time><strong>" +
            vbEsc(item.action) + (item.note ? " · " + vbEsc(item.note) : "") + "</strong></li>";
        }).join("");
      }
      setVbCount("vb-history-count", vbHistory.length);
    }

    function addVbHistory(action, note) {
      vbHistory.unshift({ at: nowStamp(), action: action, note: note || "" });
      renderVbHistory();
    }

    function setVbFlowState() {
      var ready = vbSaved && !vbDirty;
      var present = document.getElementById("vb-present");
      var send = document.getElementById("vb-send");
      var sign = document.getElementById("vb-sign");
      var hint = t("docsNeedSave");
      if (present) {
        present.disabled = !ready;
        present.title = ready ? "" : hint;
      }
      if (sign) {
        sign.disabled = !ready;
        sign.title = ready ? "" : hint;
      }
      if (send) {
        send.disabled = !(ready && vbPresented);
        send.title = !ready ? hint : (vbPresented ? "" : t("docsNeedPresent"));
      }
    }

    function markVbDirty() {
      if (vbFilling || vbDirty) return;
      vbDirty = true;
      setVbFlowState();
    }

    function requireVbSaved() {
      if (vbSaved && !vbDirty) return true;
      showToast(t("docsNeedSave"));
      return false;
    }

    var vbObjectUrl = "";
    function revokeVbPreviewUrl() {
      if (vbObjectUrl) {
        URL.revokeObjectURL(vbObjectUrl);
        vbObjectUrl = "";
      }
    }

    function setVbPreview(fileName, file) {
      var empty = document.getElementById("vb-preview-empty");
      var filePane = document.getElementById("vb-preview-file");
      var page = document.getElementById("vb-preview-page");
      var frame = document.getElementById("vb-preview-frame");
      var label = document.getElementById("vb-file-name");
      if (label) label.textContent = fileName || "Chưa chọn tệp — chọn PDF để đọc rồi nhập form";
      var hasFile = !!fileName;
      var drop = document.querySelector(".vb-file-chip");
      if (drop) {
        drop.classList.toggle("has-file", hasFile);
        var cta = drop.querySelector(".vb-file-cta");
        if (cta) cta.textContent = hasFile ? "Đổi tệp" : "Chọn PDF";
      }
      var isPdf = hasFile && /\.pdf$/i.test(fileName);
      revokeVbPreviewUrl();
      if (empty) empty.hidden = hasFile;
      if (filePane) filePane.hidden = !hasFile || isPdf;
      if (frame) {
        if (isPdf && file) {
          vbObjectUrl = URL.createObjectURL(file);
          frame.src = vbObjectUrl;
          frame.hidden = false;
        } else if (isPdf) {
          frame.src = PDF_FILE;
          frame.hidden = false;
        } else {
          frame.removeAttribute("src");
          frame.hidden = true;
        }
      }
      if (hasFile && !isPdf && page) {
        page.innerHTML = pdfPaperHtml("guide");
      }
    }

    function fillVbEditor(id) {
      var doc = id && VB_DOCS[id];
      var titleEl = document.getElementById("vb-editor-title");
      var codeEl = document.getElementById("vb-editor-code");
      var badge = document.getElementById("vb-editor-badge");
      var form = document.getElementById("vb-form");
      vbAssignees = [];
      vbPresented = false;
      vbFilling = true;
      var codeInput = document.getElementById("vb-code");
      if (codeInput) codeInput.readOnly = !!doc;
      if (!doc) {
        vbSaved = false;
        vbDirty = false;
        if (titleEl) titleEl.textContent = "Thêm văn bản";
        if (codeEl) codeEl.textContent = "—";
        if (badge) badge.textContent = "Bản nháp";
        if (form) form.reset();
        document.getElementById("vb-code").value = "";
        document.getElementById("vb-reviewer").value = "reviewer";
        vbHistory = [];
        setVbPreview("");
      } else {
        vbSaved = true;
        vbDirty = false;
        if (titleEl) titleEl.textContent = doc.title;
        if (codeEl) codeEl.textContent = doc.code;
        if (badge) badge.textContent = "Bản nháp";
        document.getElementById("vb-code").value = doc.code;
        document.getElementById("vb-title").value = doc.title;
        document.getElementById("vb-desc").value = doc.desc || "";
        document.getElementById("vb-type").value = doc.type;
        document.getElementById("vb-field").value = doc.field;
        document.getElementById("vb-urgency").value = doc.urgency;
        document.getElementById("vb-secret").value = doc.secret;
        document.getElementById("vb-reviewer").value = "reviewer";
        vbHistory = (doc.history || []).slice();
        setVbPreview((doc.pdf === "qd" ? "QD-1551.pdf" : "don-xin-nghi-phep.pdf"));
      }
      renderVbAssignees();
      renderVbHistory();
      showVbTab("info");
      vbFilling = false;
      setVbFlowState();
    }

    function showVbEditor(id) {
      fillVbEditor(id);
      if (vbList) vbList.hidden = true;
      if (vbEditor) vbEditor.hidden = false;
      window.scrollTo(0, 0);
    }

    function showVbList() {
      if (vbList) vbList.hidden = false;
      if (vbEditor) vbEditor.hidden = true;
    }

    function showVbView() {
      var hash = (location.hash || "").replace(/^#/, "");
      if (hash === "tao") {
        showVbEditor(null);
        return;
      }
      if (VB_DOCS[hash]) {
        showVbEditor(hash);
        return;
      }
      showVbList();
    }

    document.querySelectorAll("[data-vb-tab]").forEach(function (tab) {
      tab.addEventListener("click", function () {
        showVbTab(tab.getAttribute("data-vb-tab"));
      });
    });

    document.querySelectorAll("[data-vb-edit]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var id = btn.getAttribute("data-vb-edit");
        if (id) location.hash = id;
      });
    });

    var vbForm = document.getElementById("vb-form");
    if (vbForm) {
      vbForm.addEventListener("input", markVbDirty);
      vbForm.addEventListener("change", markVbDirty);
      vbForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var title = (document.getElementById("vb-title").value || "").trim();
        if (!title) {
          showToast(t("docsNeedTitle"));
          document.getElementById("vb-title").focus();
          return;
        }
        var codeEl = document.getElementById("vb-editor-code");
        var codeInput = document.getElementById("vb-code");
        var code = (codeInput && codeInput.value || "").trim();
        if (codeEl) codeEl.textContent = code || "—";
        vbSaved = true;
        vbDirty = false;
        setVbFlowState();
        addVbHistory(vbHistory.length ? "Updated" : "Created", "Đã lưu văn bản");
        var titleEl = document.getElementById("vb-editor-title");
        if (titleEl) titleEl.textContent = title;
        showToast(t("docsSaved"));
      });
    }

    var vbAssign = document.getElementById("vb-assign");
    if (vbAssign) {
      vbAssign.addEventListener("click", function () {
        var person = document.getElementById("vb-person");
        var role = document.getElementById("vb-reviewer");
        var name = person && person.value;
        if (!name) {
          showToast(t("docsNeedPerson"));
          if (person) person.focus();
          return;
        }
        var label = person.options[person.selectedIndex].text;
        vbAssignees.push({ person: label, role: (role && role.value) || "reviewer" });
        renderVbAssignees();
        markVbDirty();
        showToast(t("docsAssigned"));
      });
    }

    var assigneeList = document.getElementById("vb-assignees");
    if (assigneeList) {
      assigneeList.addEventListener("click", function (event) {
        var btn = event.target.closest("[data-unassign]");
        if (!btn) return;
        vbAssignees.splice(Number(btn.getAttribute("data-unassign")), 1);
        renderVbAssignees();
        markVbDirty();
      });
    }

    var vbPresent = document.getElementById("vb-present");
    if (vbPresent) {
      vbPresent.addEventListener("click", function () {
        if (!requireVbSaved()) return;
        vbPresented = true;
        setVbFlowState();
        addVbHistory("Presented", "Đã trình văn bản");
        showToast(t("docsPresented"));
      });
    }

    var vbSend = document.getElementById("vb-send");
    if (vbSend) {
      vbSend.addEventListener("click", function () {
        if (!requireVbSaved()) return;
        if (!vbPresented) {
          showToast(t("docsNeedPresent"));
          return;
        }
        addVbHistory("Sent", "Đã gửi văn bản");
        showToast(t("docsSent"));
      });
    }

    var vbFile = document.getElementById("vb-file");
    var vbDrop = vbFile ? vbFile.closest(".vb-drop") : null;
    if (vbFile) {
      vbFile.addEventListener("change", function () {
        var file = vbFile.files && vbFile.files[0];
        setVbPreview(file ? file.name : "", file || null);
        if (file) {
          markVbDirty();
          showToast(t("docsFilePicked"));
        }
      });
    }
    if (vbDrop && vbFile) {
      ["dragenter", "dragover"].forEach(function (type) {
        vbDrop.addEventListener(type, function (event) {
          event.preventDefault();
          vbDrop.classList.add("is-drag");
        });
      });
      ["dragleave", "drop"].forEach(function (type) {
        vbDrop.addEventListener(type, function (event) {
          event.preventDefault();
          vbDrop.classList.remove("is-drag");
        });
      });
      vbDrop.addEventListener("drop", function (event) {
        var files = event.dataTransfer && event.dataTransfer.files;
        if (!files || !files.length) return;
        try {
          vbFile.files = files;
        } catch (err) {}
        setVbPreview(files[0].name, files[0]);
        markVbDirty();
        showToast(t("docsFilePicked"));
      });
    }

    window.addEventListener("hashchange", showVbView);
    showVbView();
  }

  var wfBody = document.getElementById("wf-body");
  if (wfBody) {
    var wfQuery = "";
    var wfStatus = "all";
    var wfStepIndex = 1;
    var wfEditId = "";
    var wfMode = "create";
    var wfSteps = [];
    var wfStepEdit = -1;
    var wfList = document.getElementById("wf-list");
    var wfWizard = document.getElementById("wf-wizard");
    var wfStepModal = document.getElementById("wf-step-modal");
    var WF_TYPES = {
      leave: { code: "NP", name: "Quy trình duyệt nghỉ phép" },
      decision: { code: "QD", name: "Quy trình ban hành quyết định" },
      official: { code: "CV", name: "Quy trình xử lý công văn" }
    };
    var WF_TYPE_LABEL = { sign: "Ký", process: "Xử lý" };
    var WF_PEOPLE = ["admin", "Phó trưởng phòng", "Trưởng phòng", "hungtv"];
    var WF_ROLES = ["Người gửi", "Quản lý trực tiếp", "Lãnh đạo đơn vị"];
    var LEAVE_STEPS = [
      { code: "step01", name: "Quản lý xem duyệt", type: "sign", person: "admin", sla: 3, ret: false },
      { code: "step02", name: "Quản lý chấp nhận", type: "process", person: "Phó trưởng phòng", sla: 1, ret: false }
    ];
    var WF_TEMPLATES = {
      "np-tpl2": { type: "leave", code: "NP", name: "Quy trình duyệt nghỉ phép", fileCode: "NP-tpl2", status: "running", fileName: "Mau_trinh_ky.docx" },
      "np-tpl1": { type: "leave", code: "NP", name: "Quy trình duyệt nghỉ phép", fileCode: "NP-tpl1", status: "running", fileName: "Mau_trinh_ky.docx" },
      "np-tpl1b": { type: "leave", code: "NP", name: "Quy trình duyệt nghỉ phép", fileCode: "NP-tpl1", status: "running", fileName: "Mau_trinh_ky.docx" },
      "2062-wf": { type: "official", code: "2062", name: "Đồng bộ dữ liệu KSK định kỳ toàn dân", fileCode: "2062-WF", status: "running", fileName: "Mau_trinh_ky.docx" },
      "qd-wf-09": { type: "decision", code: "QD", name: "Quy trình ban hành quyết định", fileCode: "QD-WF-09", status: "done", fileName: "QD-WF-09.pdf" }
    };
    var WF_STATUS_LABEL = { draft: "Bản nháp", running: "Đang chạy", done: "Hoàn thành" };
    var wfObjectUrl = "";

    function cloneSteps(list) {
      return list.map(function (step) {
        return {
          code: step.code,
          name: step.name,
          type: step.type,
          person: step.person,
          sla: step.sla,
          ret: step.ret
        };
      });
    }

    function applyWfFilter() {
      var rows = wfBody.querySelectorAll("tr");
      rows.forEach(function (row) {
        var status = row.getAttribute("data-status") || "";
        var text = (row.getAttribute("data-text") || "").toLowerCase();
        var statusOk = wfStatus === "all" || status === wfStatus;
        var textOk = !wfQuery || text.indexOf(wfQuery) !== -1;
        row.hidden = !(statusOk && textOk);
      });
    }

    function fillPeople(select, values, current) {
      if (!select) return;
      select.innerHTML = '<option value="">Chọn người tham gia</option>';
      values.forEach(function (name) {
        var opt = document.createElement("option");
        opt.value = name;
        opt.textContent = name;
        if (name === current) opt.selected = true;
        select.appendChild(opt);
      });
    }

    function renderWfSteps() {
      var body = document.getElementById("wf-step-body");
      var count = document.getElementById("wf-step-count");
      if (count) {
        count.hidden = !wfSteps.length;
        count.textContent = String(wfSteps.length);
      }
      if (!body) return;
      if (!wfSteps.length) {
        body.innerHTML = '<tr><td colspan="7">Chưa có bước. Bấm Thêm bước để tạo người ký.</td></tr>';
        return;
      }
      body.innerHTML = wfSteps.map(function (step, index) {
        return '<tr>' +
          "<td>" + (index + 1) + "</td>" +
          "<td>" + step.name + "</td>" +
          "<td>" + (WF_TYPE_LABEL[step.type] || step.type) + "</td>" +
          '<td><span class="wf-person">' + (step.person || "—") + "</span></td>" +
          "<td>" + step.sla + "</td>" +
          "<td>" + (step.ret
            ? '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-label="Có"><path d="m5 12 5 5L20 7"/></svg>'
            : '<svg class="wf-return-no" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-label="Không"><path d="M6 6l12 12M18 6 6 18"/></svg>') +
          "</td>" +
          "<td><div class=\"doc-actions\">" +
          '<button class="doc-action" type="button" data-wf-step-edit="' + index + '" aria-label="Sửa bước">' +
          '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z"/></svg></button>' +
          '<button class="doc-action is-danger" type="button" data-wf-step-del="' + index + '" aria-label="Xóa bước">' +
          '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M4 7h16M9 7V5h6v2M8 7l1 12h6l1-12"/></svg></button>' +
          "</div></td></tr>";
      }).join("");
    }

    function showWfTab(name) {
      document.querySelectorAll("[data-wf-tab]").forEach(function (tab) {
        var on = tab.getAttribute("data-wf-tab") === name;
        tab.classList.toggle("is-active", on);
        tab.setAttribute("aria-selected", on ? "true" : "false");
      });
      document.querySelectorAll("#wf-wizard [data-wf-pane]").forEach(function (pane) {
        var on = pane.getAttribute("data-wf-pane") === name;
        pane.classList.toggle("is-active", on);
        pane.hidden = !on;
      });
    }

    function revokeWfPreviewUrl() {
      if (wfObjectUrl) {
        URL.revokeObjectURL(wfObjectUrl);
        wfObjectUrl = "";
      }
    }

    function setWfPreview(kind, file) {
      var empty = document.getElementById("wf-preview-empty");
      var frame = document.getElementById("wf-preview-frame");
      var paper = document.getElementById("wf-preview-file");
      var chip = document.querySelector("#wf-wizard .vb-file-chip");
      revokeWfPreviewUrl();
      if (empty) empty.hidden = kind !== "empty";
      if (paper) paper.hidden = kind !== "paper";
      if (chip) chip.classList.toggle("has-file", kind !== "empty");
      if (frame) {
        if (kind === "pdf") {
          if (file) {
            wfObjectUrl = URL.createObjectURL(file);
            frame.src = wfObjectUrl;
          } else {
            frame.src = PDF_FILE;
          }
          frame.hidden = false;
        } else {
          frame.removeAttribute("src");
          frame.hidden = true;
        }
      }
    }

    function applyTypeDefaults(forceName) {
      var typeEl = document.getElementById("wf-type");
      var meta = WF_TYPES[typeEl && typeEl.value] || WF_TYPES.leave;
      var code = document.getElementById("wf-code");
      var name = document.getElementById("wf-name-input");
      if (code) code.value = meta.code;
      if (name && (forceName || !name.value)) name.value = meta.name;
      var fileCode = document.getElementById("wf-file-code");
      var fileName = document.getElementById("wf-file-name");
      if (forceName && fileCode) fileCode.value = meta.code + "-tpl1";
      if (forceName && fileName) fileName.value = meta.name;
    }

    function fillWizard(tpl) {
      var typeEl = document.getElementById("wf-type");
      var code = document.getElementById("wf-code");
      var name = document.getElementById("wf-name-input");
      var desc = document.getElementById("wf-desc");
      var sign = document.getElementById("wf-sign-mode");
      var active = document.getElementById("wf-active");
      var fileCode = document.getElementById("wf-file-code");
      var fileName = document.getElementById("wf-file-name");
      var schema = document.getElementById("wf-schema");
      if (typeEl) typeEl.value = tpl.type;
      if (code) code.value = tpl.code;
      if (name) name.value = tpl.name;
      if (desc) desc.value = tpl.desc || "";
      if (sign) sign.value = "seq";
      if (active) active.checked = true;
      if (fileCode) fileCode.value = tpl.fileCode || (tpl.code + "-tpl1");
      if (fileName) fileName.value = tpl.name;
      if (schema) schema.value = "{}";
      var title = document.getElementById("wf-wizard-title");
      if (title) title.textContent = wfMode === "create" ? "Thêm quy trình" : (tpl.name || "Quy trình");
      var codeEl = document.getElementById("wf-editor-code");
      if (codeEl) codeEl.textContent = wfMode === "create" ? "—" : (tpl.fileCode || tpl.code || "—");
      var badge = document.getElementById("wf-editor-badge");
      if (badge) {
        var status = tpl.status || "draft";
        badge.className = "badge " + (status === "done" ? "badge-muted" : "badge-info");
        badge.textContent = WF_STATUS_LABEL[status] || "Bản nháp";
      }
      var chosen = document.getElementById("wf-file-chosen");
      var path = document.getElementById("wf-file-path");
      if (tpl.fileName) {
        if (chosen) chosen.textContent = tpl.fileName;
        if (path) path.textContent = "workflow-templates/" + tpl.fileName;
        setWfPreview(tpl.fileName.toLowerCase().indexOf(".pdf") !== -1 ? "pdf" : "paper");
      } else {
        if (chosen) chosen.textContent = "Chưa chọn tệp — chọn PDF hoặc Word để xem trước";
        if (path) path.textContent = "Chưa chọn tệp";
        setWfPreview("empty");
      }
    }

    function openWizard(mode, id) {
      wfMode = mode;
      wfEditId = id || "";
      var tpl = (id && WF_TEMPLATES[id]) || {
        type: "leave",
        code: "NP",
        name: "Quy trình duyệt nghỉ phép",
        fileCode: "NP-tpl1",
        status: "draft"
      };
      fillWizard(tpl);
      wfSteps = mode === "create" ? [] : cloneSteps(LEAVE_STEPS);
      if (id === "qd-wf-09") {
        wfSteps = cloneSteps([
          { code: "step01", name: "Soạn thảo", type: "process", person: "hungtv", sla: 2, ret: false },
          { code: "step02", name: "Phê duyệt", type: "sign", person: "Trưởng phòng", sla: 1, ret: true },
          { code: "step03", name: "Ban hành", type: "process", person: "admin", sla: 1, ret: false }
        ]);
      }
      renderWfSteps();
      if (wfList) wfList.hidden = true;
      if (wfWizard) wfWizard.hidden = false;
      showWfTab("info");
      window.scrollTo(0, 0);
    }

    function closeWizard() {
      if (wfList) wfList.hidden = false;
      if (wfWizard) wfWizard.hidden = true;
    }

    function showWfView() {
      var hash = (location.hash || "").replace(/^#/, "");
      if (hash === "tao") {
        openWizard("create");
        return;
      }
      if (WF_TEMPLATES[hash]) {
        openWizard("edit", hash);
        return;
      }
      if (hash === "sua") {
        openWizard("edit", wfEditId || "np-tpl2");
        return;
      }
      closeWizard();
      wfStatus = hash === "theo-doi" ? "running" : "all";
      applyWfFilter();
    }

    function closeWfStepModal() {
      if (!wfStepModal) return;
      wfStepModal.classList.remove("is-open");
      wfStepModal.hidden = true;
      document.body.classList.remove("modal-open");
      wfStepEdit = -1;
    }

    function openWfStepModal(index) {
      if (!wfStepModal) return;
      var form = document.getElementById("wf-step-form");
      var title = document.getElementById("wf-step-modal-title");
      var nameErr = document.getElementById("wf-step-name-err");
      var assign = document.querySelector('input[name="wf-assign"][value="person"]');
      wfStepEdit = typeof index === "number" ? index : -1;
      if (form) form.reset();
      if (nameErr) nameErr.classList.remove("is-visible");
      var nameInput = document.getElementById("wf-step-name");
      if (nameInput) nameInput.classList.remove("is-invalid");
      if (wfStepEdit >= 0) {
        var step = wfSteps[wfStepEdit];
        if (title) title.textContent = "Sửa bước";
        document.getElementById("wf-step-code").value = step.code;
        document.getElementById("wf-step-name").value = step.name;
        document.getElementById("wf-step-type").value = step.type;
        document.getElementById("wf-step-sla").value = step.sla;
        document.getElementById("wf-step-return").checked = !!step.ret;
        fillPeople(document.getElementById("wf-step-person"), WF_PEOPLE, step.person);
      } else {
        if (title) title.textContent = "Thêm bước";
        document.getElementById("wf-step-code").value = "step0" + (wfSteps.length + 1);
        document.getElementById("wf-step-sla").value = "1";
        document.getElementById("wf-step-type").value = "process";
        fillPeople(document.getElementById("wf-step-person"), WF_PEOPLE, "");
      }
      if (assign) assign.checked = true;
      wfStepModal.hidden = false;
      wfStepModal.classList.add("is-open");
      document.body.classList.add("modal-open");
      var first = document.getElementById("wf-step-code");
      if (first) first.focus();
    }

    showWfView();
    window.addEventListener("hashchange", showWfView);

    var wfForm = document.getElementById("wf-search-form");
    if (wfForm) {
      wfForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var input = document.getElementById("wf-q");
        wfQuery = ((input && input.value) || "").trim().toLowerCase();
        applyWfFilter();
        showToast(t("wfFiltered"));
      });
    }

    var wfRefresh = document.getElementById("refresh-wf");
    if (wfRefresh) {
      wfRefresh.addEventListener("click", function () {
        var input = document.getElementById("wf-q");
        if (input) input.value = "";
        wfQuery = "";
        wfStatus = "all";
        if (location.hash === "#theo-doi") {
          history.replaceState(null, "", "quy-trinh.html");
        }
        applyWfFilter();
        showToast(t("wfRefreshed"));
      });
    }

    var wfFilterBtn = document.getElementById("filter-btn");
    var wfFilterMenu = document.getElementById("filter-menu");
    if (wfFilterBtn && wfFilterMenu) {
      wfFilterBtn.addEventListener("click", function (event) {
        event.stopPropagation();
        var open = !wfFilterMenu.classList.contains("is-open");
        wfFilterMenu.classList.toggle("is-open", open);
        wfFilterBtn.setAttribute("aria-expanded", open ? "true" : "false");
      });
      wfFilterMenu.querySelectorAll("[data-wf-status]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          wfStatus = btn.getAttribute("data-wf-status") || "all";
          applyWfFilter();
          wfFilterMenu.classList.remove("is-open");
          wfFilterBtn.setAttribute("aria-expanded", "false");
          showToast(t("wfFiltered"));
        });
      });
    }

    var wfPageSize = document.getElementById("wf-page-size");
    if (wfPageSize) {
      wfPageSize.addEventListener("change", function () {
        showToast(t("wfFiltered"));
      });
    }

    document.querySelectorAll("[data-wf-edit]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var id = btn.getAttribute("data-wf-edit") || "np-tpl2";
        location.hash = id;
      });
    });

    document.querySelectorAll("[data-wf-tab]").forEach(function (tab) {
      tab.addEventListener("click", function () {
        showWfTab(tab.getAttribute("data-wf-tab") || "info");
      });
    });

    var wfType = document.getElementById("wf-type");
    if (wfType) {
      wfType.addEventListener("change", function () {
        applyTypeDefaults(true);
      });
    }

    var wfSignMode = document.getElementById("wf-sign-mode");
    if (wfSignMode) {
      wfSignMode.addEventListener("change", function () {
        var fileSign = document.getElementById("wf-file-sign");
        if (fileSign) fileSign.value = wfSignMode.value;
      });
    }

    var wfFormEl = document.getElementById("wf-form");
    if (wfFormEl) {
      wfFormEl.addEventListener("submit", function (event) {
        event.preventDefault();
        var title = document.getElementById("wf-wizard-title");
        var name = document.getElementById("wf-name-input");
        var codeEl = document.getElementById("wf-editor-code");
        var fileCode = document.getElementById("wf-file-code");
        if (title && name && name.value && wfMode !== "create") title.textContent = name.value;
        if (codeEl && fileCode && fileCode.value) codeEl.textContent = fileCode.value;
        showToast(t("wfSaved"));
      });
    }

    var wfWord = document.getElementById("wf-word");
    if (wfWord) {
      wfWord.addEventListener("change", function () {
        var file = wfWord.files && wfWord.files[0];
        var chosen = document.getElementById("wf-file-chosen");
        var path = document.getElementById("wf-file-path");
        if (!file) return;
        if (chosen) chosen.textContent = file.name;
        if (path) path.textContent = "workflow-templates/" + file.name;
        var lower = file.name.toLowerCase();
        if (lower.slice(-4) === ".pdf") setWfPreview("pdf", file);
        else setWfPreview("paper");
        showToast(t("wfFilePicked"));
      });
    }

    var wfAddStep = document.getElementById("wf-add-step");
    if (wfAddStep) {
      wfAddStep.addEventListener("click", function () {
        openWfStepModal();
      });
    }

    var wfStepBody = document.getElementById("wf-step-body");
    if (wfStepBody) {
      wfStepBody.addEventListener("click", function (event) {
        var editBtn = event.target.closest("[data-wf-step-edit]");
        var delBtn = event.target.closest("[data-wf-step-del]");
        if (editBtn) {
          openWfStepModal(Number(editBtn.getAttribute("data-wf-step-edit")));
          return;
        }
        if (delBtn) {
          var idx = Number(delBtn.getAttribute("data-wf-step-del"));
          wfSteps.splice(idx, 1);
          renderWfSteps();
          showToast(t("wfStepDeleted"));
        }
      });
    }

    document.querySelectorAll("[data-wf-step-close]").forEach(function (btn) {
      btn.addEventListener("click", closeWfStepModal);
    });

    if (wfStepModal) {
      wfStepModal.addEventListener("click", function (event) {
        if (event.target === wfStepModal) closeWfStepModal();
      });
    }

    document.querySelectorAll('input[name="wf-assign"]').forEach(function (radio) {
      radio.addEventListener("change", function () {
        var list = radio.value === "role" ? WF_ROLES : WF_PEOPLE;
        fillPeople(document.getElementById("wf-step-person"), list, "");
      });
    });

    var wfStepForm = document.getElementById("wf-step-form");
    if (wfStepForm) {
      wfStepForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var nameInput = document.getElementById("wf-step-name");
        var nameErr = document.getElementById("wf-step-name-err");
        var person = document.getElementById("wf-step-person");
        var name = ((nameInput && nameInput.value) || "").trim();
        if (!name) {
          if (nameInput) nameInput.classList.add("is-invalid");
          if (nameErr) nameErr.classList.add("is-visible");
          if (nameInput) nameInput.focus();
          showToast(t("wfStepNeedName"));
          return;
        }
        if (person && !person.value) {
          showToast(t("wfNeedPerson"));
          person.focus();
          return;
        }
        var next = {
          code: document.getElementById("wf-step-code").value || ("step0" + (wfSteps.length + 1)),
          name: name,
          type: document.getElementById("wf-step-type").value,
          person: person.value,
          sla: Number(document.getElementById("wf-step-sla").value) || 1,
          ret: document.getElementById("wf-step-return").checked
        };
        if (wfStepEdit >= 0) wfSteps[wfStepEdit] = next;
        else wfSteps.push(next);
        renderWfSteps();
        closeWfStepModal();
        showToast(t("wfStepSaved"));
      });
    }
  }

  var pjBody = document.getElementById("pj-body");
  if (pjBody) {
    var pjQuery = "";
    var pjStatus = "all";

    function applyPjFilter() {
      pjBody.querySelectorAll("tr").forEach(function (row) {
        var status = row.getAttribute("data-status") || "";
        var text = (row.getAttribute("data-text") || "").toLowerCase();
        row.hidden = !((pjStatus === "all" || status === pjStatus) && (!pjQuery || text.indexOf(pjQuery) !== -1));
      });
    }

    var pjForm = document.getElementById("pj-search-form");
    if (pjForm) {
      pjForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var input = document.getElementById("pj-q");
        pjQuery = ((input && input.value) || "").trim().toLowerCase();
        applyPjFilter();
        showToast(t("pjFiltered"));
      });
    }

    var pjFilterBtn = document.getElementById("filter-btn");
    var pjFilterMenu = document.getElementById("filter-menu");
    if (pjFilterBtn && pjFilterMenu) {
      pjFilterBtn.addEventListener("click", function (event) {
        event.stopPropagation();
        var open = !pjFilterMenu.classList.contains("is-open");
        pjFilterMenu.classList.toggle("is-open", open);
        pjFilterBtn.setAttribute("aria-expanded", open ? "true" : "false");
      });
      pjFilterMenu.querySelectorAll("[data-pj-status]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          pjStatus = btn.getAttribute("data-pj-status") || "all";
          applyPjFilter();
          pjFilterMenu.classList.remove("is-open");
          pjFilterBtn.setAttribute("aria-expanded", "false");
          showToast(t("pjFiltered"));
        });
      });
    }

    var pjAll = document.getElementById("pj-check-all");
    if (pjAll) {
      pjAll.addEventListener("change", function () {
        pjBody.querySelectorAll('input[type="checkbox"]').forEach(function (box) {
          box.checked = pjAll.checked;
        });
      });
    }
  }

  var dmBody = document.getElementById("dm-body");
  if (dmBody) {
    var dmQuery = "";
    var dmStatus = "all";
    var dmModal = document.getElementById("dm-modal");

    function applyDmFilter() {
      dmBody.querySelectorAll("tr").forEach(function (row) {
        var status = row.getAttribute("data-status") || "";
        var text = (row.getAttribute("data-text") || "").toLowerCase();
        row.hidden = !((dmStatus === "all" || status === dmStatus) && (!dmQuery || text.indexOf(dmQuery) !== -1));
      });
    }

    function closeDmModal() {
      if (!dmModal) return;
      dmModal.classList.remove("is-open");
      dmModal.hidden = true;
      document.body.classList.remove("modal-open");
    }

    function openDmModal(row) {
      if (!dmModal) return;
      var title = document.getElementById("dm-modal-title");
      var id = document.getElementById("dm-id");
      var code = document.getElementById("dm-code");
      var name = document.getElementById("dm-name");
      var parent = document.getElementById("dm-parent");
      var sort = document.getElementById("dm-sort");
      var active = document.getElementById("dm-active");
      if (row) {
        if (title) title.textContent = "Sửa phòng ban";
        if (id) id.value = row.getAttribute("data-id") || "";
        if (code) code.value = (row.querySelector(".doc-code") || {}).textContent || "";
        if (name) name.value = (row.children[2] && row.children[2].textContent) || "";
        if (parent) {
          var parentText = ((row.children[3] && row.children[3].textContent) || "").trim();
          parent.value = parentText.indexOf("BVBĐ") === 0 ? "BVBĐ" : parentText.indexOf("CQBBN") === 0 ? "CQBBN" : "";
        }
        if (sort) sort.value = (row.children[4] && row.children[4].textContent) || "0";
        if (active) active.value = row.getAttribute("data-status") || "on";
      } else {
        if (title) title.textContent = "Thêm phòng ban";
        if (id) id.value = "";
        if (code) code.value = "";
        if (name) name.value = "";
        if (parent) parent.value = "";
        if (sort) sort.value = String(dmBody.querySelectorAll("tr").length);
        if (active) active.value = "on";
      }
      dmModal.hidden = false;
      dmModal.classList.add("is-open");
      document.body.classList.add("modal-open");
      if (code) code.focus();
    }

    var dmForm = document.getElementById("dm-search-form");
    if (dmForm) {
      dmForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var input = document.getElementById("dm-q");
        dmQuery = ((input && input.value) || "").trim().toLowerCase();
        applyDmFilter();
        showToast(t("dmFiltered"));
      });
    }

    var dmFilterBtn = document.getElementById("filter-btn");
    var dmFilterMenu = document.getElementById("filter-menu");
    if (dmFilterBtn && dmFilterMenu) {
      dmFilterBtn.addEventListener("click", function () {
        var open = !dmFilterMenu.classList.contains("is-open");
        dmFilterMenu.classList.toggle("is-open", open);
        dmFilterBtn.setAttribute("aria-expanded", open ? "true" : "false");
      });
      dmFilterMenu.querySelectorAll("[data-dm-status]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          dmStatus = btn.getAttribute("data-dm-status") || "all";
          applyDmFilter();
          dmFilterMenu.classList.remove("is-open");
          dmFilterBtn.setAttribute("aria-expanded", "false");
          showToast(t("dmFiltered"));
        });
      });
    }

    var dmAll = document.getElementById("dm-check-all");
    if (dmAll) {
      dmAll.addEventListener("change", function () {
        dmBody.querySelectorAll('input[type="checkbox"]').forEach(function (box) {
          box.checked = dmAll.checked;
        });
      });
    }

    var dmAdd = document.getElementById("dm-add");
    if (dmAdd) dmAdd.addEventListener("click", function () { openDmModal(null); });

    dmBody.addEventListener("click", function (event) {
      var edit = event.target.closest("[data-dm-edit]");
      if (edit) {
        openDmModal(edit.closest("tr"));
        return;
      }
      var del = event.target.closest("[data-dm-del]");
      if (del) {
        var row = del.closest("tr");
        if (row) row.hidden = true;
        showToast(t("dmDeleted"));
      }
    });

    document.querySelectorAll("[data-dm-close]").forEach(function (btn) {
      btn.addEventListener("click", closeDmModal);
    });
    if (dmModal) {
      dmModal.addEventListener("click", function (event) {
        if (event.target === dmModal) closeDmModal();
      });
    }

    var dmSaveForm = document.getElementById("dm-form");
    if (dmSaveForm) {
      dmSaveForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var code = ((document.getElementById("dm-code") || {}).value || "").trim();
        var name = ((document.getElementById("dm-name") || {}).value || "").trim();
        if (!code || !name) {
          showToast(t("dmNeedCode"));
          return;
        }
        closeDmModal();
        showToast(t("dmSaved"));
      });
    }
  }

  var cvKanban = document.getElementById("cv-kanban");
  if (cvKanban) {
    var cvQuery = "";
    var cvStatus = "all";
    var cvList = document.getElementById("cv-list");
    var CV_LABEL = { todo: "Cần làm", doing: "Đang làm", wait: "Đang chờ", done: "Hoàn thành", cancel: "Đã hủy" };
    var CV_BADGE = { todo: "badge-warn", doing: "badge-info", wait: "badge-muted", done: "badge-muted", cancel: "badge-overdue" };

    function updateKbCounts() {
      cvKanban.querySelectorAll("[data-kb-col]").forEach(function (col) {
        var count = col.querySelectorAll(".kb-card:not([hidden])").length;
        var el = col.querySelector(".kb-count");
        if (el) el.textContent = String(count);
      });
    }

    function applyCvFilter() {
      cvKanban.querySelectorAll(".kb-card").forEach(function (card) {
        var status = card.getAttribute("data-status") || "";
        var text = (card.getAttribute("data-text") || "").toLowerCase();
        card.hidden = !((cvStatus === "all" || status === cvStatus) && (!cvQuery || text.indexOf(cvQuery) !== -1));
      });
      document.querySelectorAll("#cv-body tr").forEach(function (row) {
        var status = row.getAttribute("data-status") || "";
        var text = (row.getAttribute("data-text") || "").toLowerCase();
        row.hidden = !((cvStatus === "all" || status === cvStatus) && (!cvQuery || text.indexOf(cvQuery) !== -1));
      });
      updateKbCounts();
    }

    function showCvView() {
      var list = location.hash === "#danh-sach";
      if (cvKanban) cvKanban.hidden = list;
      if (cvList) cvList.hidden = !list;
      var toggle = document.getElementById("cv-view-toggle");
      var label = document.getElementById("cv-view-label");
      if (toggle) toggle.href = list ? "cong-viec.html" : "#danh-sach";
      if (label) label.textContent = list ? "Kanban" : "Danh sách";
    }

    function syncCvRow(id, status) {
      var row = document.querySelector('#cv-body tr[data-id="' + id + '"]');
      if (!row) return;
      row.setAttribute("data-status", status);
      var badge = row.querySelector(".badge");
      if (badge) {
        badge.className = "badge " + (CV_BADGE[status] || "badge-muted");
        badge.textContent = CV_LABEL[status] || status;
      }
    }

    showCvView();
    window.addEventListener("hashchange", showCvView);

    var cvForm = document.getElementById("cv-search-form");
    if (cvForm) {
      cvForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var input = document.getElementById("cv-q");
        cvQuery = ((input && input.value) || "").trim().toLowerCase();
        applyCvFilter();
        showToast(t("cvFiltered"));
      });
    }

    var cvFilterBtn = document.getElementById("filter-btn");
    var cvFilterMenu = document.getElementById("filter-menu");
    if (cvFilterBtn && cvFilterMenu) {
      cvFilterBtn.addEventListener("click", function (event) {
        event.stopPropagation();
        var open = !cvFilterMenu.classList.contains("is-open");
        cvFilterMenu.classList.toggle("is-open", open);
        cvFilterBtn.setAttribute("aria-expanded", open ? "true" : "false");
      });
      cvFilterMenu.querySelectorAll("[data-cv-status]").forEach(function (btn) {
        btn.addEventListener("click", function () {
          cvStatus = btn.getAttribute("data-cv-status") || "all";
          applyCvFilter();
          cvFilterMenu.classList.remove("is-open");
          cvFilterBtn.setAttribute("aria-expanded", "false");
          showToast(t("cvFiltered"));
        });
      });
    }

    cvKanban.querySelectorAll(".kb-card").forEach(function (card) {
      card.addEventListener("dragstart", function (event) {
        event.dataTransfer.setData("text/plain", card.getAttribute("data-id") || "");
        event.dataTransfer.effectAllowed = "move";
        card.classList.add("is-dragging");
      });
      card.addEventListener("dragend", function () {
        card.classList.remove("is-dragging");
        cvKanban.querySelectorAll(".kb-col").forEach(function (col) {
          col.classList.remove("is-drop");
        });
      });
    });

    cvKanban.querySelectorAll("[data-kb-col]").forEach(function (col) {
      col.addEventListener("dragover", function (event) {
        event.preventDefault();
        col.classList.add("is-drop");
      });
      col.addEventListener("dragleave", function () {
        col.classList.remove("is-drop");
      });
      col.addEventListener("drop", function (event) {
        event.preventDefault();
        col.classList.remove("is-drop");
        var id = event.dataTransfer.getData("text/plain");
        var card = cvKanban.querySelector('.kb-card[data-id="' + id + '"]');
        var body = col.querySelector(".kb-col-body");
        if (!card || !body) return;
        var status = col.getAttribute("data-kb-col") || "todo";
        body.appendChild(card);
        card.setAttribute("data-status", status);
        syncCvRow(id, status);
        updateKbCounts();
        showToast(t("cvMoved"));
      });
    });
  }

  var chatShell = document.getElementById("chat-shell");
  if (chatShell) {
    var activeChat = "phi-long";
    var chatInfo = document.getElementById("chat-info");
    var moreBtn = document.getElementById("chat-more-btn");
    var chatMeta = {
      "phi-long": {
        kindLabel: "Trao đổi riêng",
        members: [
          { initials: "NH", name: "nguyễn hồ phi long", role: "Thành viên" },
          { initials: "B", name: "Bạn", role: "Quản trị viên", you: true }
        ],
        photos: [
          { name: "ghim-hoi-thoai.jpg", tone: "teal" },
          { name: "anh-trao-doi.jpg", tone: "sand" }
        ],
        files: [{ name: "Ghi_chu.txt", ext: "TXT" }],
        pinnedMsg: { id: "pl-3", text: "test ghim hội thoại" }
      },
      "nhom": {
        kindLabel: "Chat nhóm",
        members: [
          { initials: "TH", name: "Trần Việt Hùng", role: "Thành viên" },
          { initials: "NH", name: "nguyễn hồ phi long", role: "Thành viên" },
          { initials: "PT", name: "Phó trưởng phòng", role: "Thành viên" },
          { initials: "AD", name: "admin", role: "Quản trị viên" },
          { initials: "B", name: "Bạn", role: "Quản trị viên", you: true }
        ],
        photos: [
          { name: "anh-cuoc-hop.jpg", tone: "teal" },
          { name: "anh-van-phong.jpg", tone: "blue" },
          { name: "bien-ban-scan.jpg", tone: "sand" },
          { name: "so-do-quy-trinh.png", tone: "mint" }
        ],
        files: [
          { name: "Bien_ban_hop.pdf", ext: "PDF" },
          { name: "Cong_van_den.docx", ext: "DOC" },
          { name: "Danh_sach.xlsx", ext: "XLS" }
        ]
      },
      "pho-truong": {
        kindLabel: "Trao đổi riêng",
        members: [
          { initials: "PT", name: "Phó trưởng phòng", role: "Thành viên" },
          { initials: "B", name: "Bạn", role: "Quản trị viên", you: true }
        ],
        photos: [],
        files: []
      }
    };

    function cancelRename() {
      var start = document.getElementById("chat-rename-start");
      var input = document.getElementById("chat-rename");
      if (start) start.hidden = false;
      if (input) {
        input.hidden = true;
        input.value = "";
      }
    }

    function closeChatInfo() {
      cancelRename();
      chatShell.classList.remove("is-info-open");
      if (chatInfo) chatInfo.hidden = true;
      if (moreBtn) {
        moreBtn.classList.remove("is-active");
        moreBtn.setAttribute("aria-expanded", "false");
      }
    }

    var MSG_MORE =
      '<button class="msg-more" type="button" aria-haspopup="menu" aria-expanded="false" aria-label="Thao tác tin nhắn">' +
      '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><circle cx="12" cy="5" r="1.3"/><circle cx="12" cy="12" r="1.3"/><circle cx="12" cy="19" r="1.3"/></svg>' +
      "</button>";
    var ICO = {
      pin: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M12 17v5"/><path d="M8 3h8l-1 7a4 4 0 1 1-6 0z"/></svg>',
      forward: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M15 17l5-5-5-5"/><path d="M4 18v-2a4 4 0 0 1 4-4h12"/></svg>',
      task: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M9 6h11M9 12h11M9 18h11"/><path d="M4 6h.01M4 12h.01M4 18h.01"/></svg>',
      recall: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M3 12a9 9 0 1 0 3-6.7"/><path d="M3 4v5h5"/></svg>',
      trash: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M4 7h16"/><path d="M9 7V5h6v2"/><path d="M6 7l1 13h10l1-13"/></svg>',
      check: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="m5 12 5 5L20 7"/></svg>'
    };
    var msgMenuEl = document.getElementById("msg-menu");
    var chatDialog = document.getElementById("chat-dialog");
    var msgMenuAnchor = null;
    var dialogState = { view: "", payload: null, target: "", kind: "" };

    function esc(value) {
      return String(value == null ? "" : value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
    }

    function nextMsgId() {
      return "m-" + Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
    }

    function ensureMeta(chatId) {
      if (!chatMeta[chatId]) {
        chatMeta[chatId] = { kindLabel: "Trao đổi riêng", members: [], photos: [], files: [] };
      }
      return chatMeta[chatId];
    }

    function msgPreview(el) {
      if (!el) return "";
      if (el.classList.contains("is-recalled")) return "Tin nhắn đã được thu hồi";
      var type = el.getAttribute("data-type") || "text";
      var file = el.getAttribute("data-file") || "";
      if (type === "image") return "[Hình ảnh] " + (file || "Ảnh");
      if (type === "file") return "[Tệp] " + (file || "Tệp");
      if (type === "task") {
        var titleEl = el.querySelector(".msg-task-card strong");
        return titleEl ? "[Việc] " + titleEl.textContent : "[Công việc]";
      }
      var bubble = el.querySelector(".msg-bubble");
      if (!bubble) return "";
      var clone = bubble.cloneNode(true);
      clone.querySelectorAll(".msg-name, .msg-fwd-label, .msg-task-label").forEach(function (node) {
        node.remove();
      });
      return (clone.textContent || "").replace(/\s+/g, " ").trim();
    }

    function msgPayload(el) {
      if (!el) return null;
      var preview = msgPreview(el);
      return {
        el: el,
        id: el.getAttribute("data-msg-id"),
        type: el.getAttribute("data-type") || "text",
        author: el.getAttribute("data-author") || "",
        isOut: el.classList.contains("is-out"),
        recalled: el.classList.contains("is-recalled"),
        pinned: el.classList.contains("is-pinned"),
        file: el.getAttribute("data-file") || "",
        ext: el.getAttribute("data-ext") || "",
        tone: el.getAttribute("data-tone") || "teal",
        text: preview,
        preview: preview
      };
    }

    function updateListPreview(chatId, text) {
      var preview = document.querySelector('#chat-list-chats [data-chat="' + chatId + '"] .chat-item-preview');
      if (preview) preview.textContent = text || "Chưa có tin nhắn";
    }

    function updateListPreviewFromLast(pane) {
      if (!pane) return;
      var last = null;
      pane.querySelectorAll(".msg").forEach(function (msg) { last = msg; });
      updateListPreview(pane.getAttribute("data-chat-pane"), last ? msgPreview(last) : "Chưa có tin nhắn");
    }

    function appendOutgoing(chatId, attrs, bubbleHtml, bubbleClass) {
      var pane = document.querySelector('[data-chat-pane="' + chatId + '"]');
      if (!pane) return null;
      var empty = pane.querySelector(".chat-empty");
      if (empty) empty.remove();
      var row = document.createElement("div");
      row.className = "msg is-out";
      row.setAttribute("data-msg-id", (attrs && attrs.id) || nextMsgId());
      row.setAttribute("data-type", (attrs && attrs.type) || "text");
      row.setAttribute("data-author", "me");
      if (attrs && attrs.file) row.setAttribute("data-file", attrs.file);
      if (attrs && attrs.ext) row.setAttribute("data-ext", attrs.ext);
      if (attrs && attrs.tone) row.setAttribute("data-tone", attrs.tone);
      row.innerHTML =
        '<div class="msg-stack">' + MSG_MORE +
        '<div class="msg-bubble' + (bubbleClass ? " " + bubbleClass : "") + '">' + bubbleHtml + "</div></div>";
      pane.appendChild(row);
      if (chatId === activeChat) pane.scrollTop = pane.scrollHeight;
      updateListPreview(chatId, msgPreview(row));
      return row;
    }

    function rememberMedia(chatId, payload) {
      var meta = ensureMeta(chatId);
      if (!payload) return;
      if (payload.type === "image" && payload.file) {
        meta.photos = meta.photos || [];
        if (!meta.photos.some(function (item) { return item.name === payload.file; })) {
          meta.photos.push({ name: payload.file, tone: payload.tone || "teal" });
        }
      }
      if (payload.type === "file" && payload.file) {
        meta.files = meta.files || [];
        if (!meta.files.some(function (item) { return item.name === payload.file; })) {
          meta.files.push({ name: payload.file, ext: payload.ext || "FILE" });
        }
      }
    }

    function sortPinnedChats() {
      var list = document.getElementById("chat-list-chats");
      if (!list) return;
      var items = Array.prototype.slice.call(list.querySelectorAll(".chat-item"));
      items.sort(function (a, b) {
        var ap = a.classList.contains("is-pinned") ? 0 : 1;
        var bp = b.classList.contains("is-pinned") ? 0 : 1;
        if (ap !== bp) return ap - bp;
        return 0;
      });
      items.forEach(function (el) { list.appendChild(el); });
    }

    function syncPinUI() {
      var item = document.querySelector('#chat-list-chats [data-chat="' + activeChat + '"]');
      var meta = chatMeta[activeChat] || {};
      var pinnedConv = !!(item && item.classList.contains("is-pinned"));
      var pinBtn = document.getElementById("chat-pin-conv");
      var infoPin = document.getElementById("chat-info-pin");
      var bar = document.getElementById("chat-pin-bar");
      var preview = document.getElementById("chat-pin-preview");
      var pins = document.getElementById("chat-info-pins");
      if (pinBtn) {
        pinBtn.classList.toggle("is-active", pinnedConv);
        pinBtn.setAttribute("aria-pressed", pinnedConv ? "true" : "false");
        pinBtn.setAttribute("aria-label", pinnedConv ? "Bỏ ghim hội thoại" : "Ghim hội thoại");
      }
      if (infoPin) {
        var pinLabel = infoPin.querySelector("span");
        if (pinLabel) pinLabel.textContent = pinnedConv ? "Bỏ ghim hội thoại" : "Ghim hội thoại";
      }
      if (bar && preview) {
        if (meta.pinnedMsg) {
          bar.hidden = false;
          preview.textContent = meta.pinnedMsg.text;
        } else {
          bar.hidden = true;
        }
      }
      if (pins) {
        pins.innerHTML = "";
        if (meta.pinnedMsg) {
          var pinItem = document.createElement("button");
          pinItem.type = "button";
          pinItem.className = "chat-pin-info";
          pinItem.innerHTML = "<strong>Tin đã ghim</strong><span>" + esc(meta.pinnedMsg.text) + "</span>";
          pinItem.addEventListener("click", function () {
            closeChatInfo();
            jumpToPinned();
          });
          pins.appendChild(pinItem);
        } else {
          var empty = document.createElement("p");
          empty.className = "chat-media-empty";
          empty.textContent = t("chatNoPins");
          pins.appendChild(empty);
        }
      }
    }

    function togglePinConv() {
      var item = document.querySelector('#chat-list-chats [data-chat="' + activeChat + '"]');
      if (!item) return;
      var on = !item.classList.contains("is-pinned");
      item.classList.toggle("is-pinned", on);
      var mark = item.querySelector(".chat-pin-mark");
      if (mark) mark.hidden = !on;
      sortPinnedChats();
      syncPinUI();
      showToast(on ? t("chatPinnedConv") : t("chatUnpinnedConv"));
    }

    function pinMessage(msgEl, on) {
      if (!msgEl) return;
      var pane = msgEl.closest("[data-chat-pane]");
      var chatId = pane && pane.getAttribute("data-chat-pane");
      if (!chatId) return;
      var meta = ensureMeta(chatId);
      if (pane) {
        pane.querySelectorAll(".msg.is-pinned").forEach(function (el) {
          el.classList.remove("is-pinned");
        });
      }
      if (on) {
        msgEl.classList.add("is-pinned");
        meta.pinnedMsg = {
          id: msgEl.getAttribute("data-msg-id"),
          text: msgPreview(msgEl),
          type: msgEl.getAttribute("data-type") || "text"
        };
      } else {
        meta.pinnedMsg = null;
      }
      if (chatId === activeChat) syncPinUI();
      showToast(on ? t("chatPinned") : t("chatUnpinned"));
    }

    function unpinCurrent() {
      var meta = chatMeta[activeChat];
      if (!meta || !meta.pinnedMsg) return;
      var el = document.querySelector('[data-chat-pane="' + activeChat + '"] [data-msg-id="' + meta.pinnedMsg.id + '"]');
      if (el) el.classList.remove("is-pinned");
      meta.pinnedMsg = null;
      syncPinUI();
      showToast(t("chatUnpinned"));
    }

    function jumpToPinned() {
      var meta = chatMeta[activeChat];
      if (!meta || !meta.pinnedMsg) return;
      var el = document.querySelector('[data-chat-pane="' + activeChat + '"] [data-msg-id="' + meta.pinnedMsg.id + '"]');
      if (!el) return;
      el.scrollIntoView({ block: "center", behavior: "smooth" });
      el.classList.add("is-flash");
      window.setTimeout(function () { el.classList.remove("is-flash"); }, 1100);
    }

    function recallMessage(el) {
      if (!el) return;
      el.classList.add("is-recalled");
      el.removeAttribute("data-file");
      var bubble = el.querySelector(".msg-bubble");
      if (bubble) {
        bubble.className = "msg-bubble";
        bubble.textContent = "Tin nhắn đã được thu hồi";
      }
      if (el.classList.contains("is-pinned")) {
        el.classList.remove("is-pinned");
        var pane = el.closest("[data-chat-pane]");
        var chatId = pane && pane.getAttribute("data-chat-pane");
        if (chatId && chatMeta[chatId]) chatMeta[chatId].pinnedMsg = null;
        if (chatId === activeChat) syncPinUI();
      }
      updateListPreviewFromLast(el.closest("[data-chat-pane]"));
      showToast(t("chatRecalled"));
    }

    function deleteMessage(el) {
      if (!el) return;
      var pane = el.closest("[data-chat-pane]");
      var chatId = pane && pane.getAttribute("data-chat-pane");
      var msgId = el.getAttribute("data-msg-id");
      if (chatId && chatMeta[chatId] && chatMeta[chatId].pinnedMsg && chatMeta[chatId].pinnedMsg.id === msgId) {
        chatMeta[chatId].pinnedMsg = null;
        if (chatId === activeChat) syncPinUI();
      }
      el.remove();
      if (pane && !pane.querySelector(".msg")) {
        var empty = document.createElement("p");
        empty.className = "chat-empty";
        empty.textContent = "Chưa có tin nhắn";
        pane.appendChild(empty);
      }
      updateListPreviewFromLast(pane);
      showToast(t("chatDeleted"));
    }

    function closeMsgMenu() {
      if (msgMenuEl) msgMenuEl.hidden = true;
      document.querySelectorAll(".msg-more[aria-expanded='true']").forEach(function (btn) {
        btn.setAttribute("aria-expanded", "false");
      });
      msgMenuAnchor = null;
    }

    function positionMenu(menu, btn) {
      var rect = btn.getBoundingClientRect();
      var width = menu.offsetWidth || 220;
      var height = menu.offsetHeight || 200;
      var left = rect.left;
      var top = rect.bottom + 6;
      if (left + width > window.innerWidth - 8) left = Math.max(8, window.innerWidth - width - 8);
      if (top + height > window.innerHeight - 8) top = Math.max(8, rect.top - height - 6);
      menu.style.left = left + "px";
      menu.style.top = top + "px";
    }

    function openMsgMenu(btn, payload) {
      if (!msgMenuEl || !payload) return;
      if (msgMenuAnchor === btn && !msgMenuEl.hidden) {
        closeMsgMenu();
        return;
      }
      closeMsgMenu();
      msgMenuAnchor = btn;
      if (btn && btn.classList.contains("msg-more")) btn.setAttribute("aria-expanded", "true");
      var items = [];
      if (!payload.recalled) {
        if (payload.el) {
          items.push({ action: "pin", label: payload.pinned ? "Bỏ ghim" : "Ghim tin nhắn", icon: ICO.pin });
        }
        items.push({ action: "forward", label: "Chuyển tiếp", icon: ICO.forward });
        items.push({ action: "task", label: "Giao việc", icon: ICO.task });
        if (payload.el && payload.isOut) {
          items.push({ action: "recall", label: "Thu hồi", icon: ICO.recall, danger: true });
        }
      }
      if (payload.el) items.push({ action: "delete", label: "Xóa", icon: ICO.trash, danger: true });
      msgMenuEl.innerHTML = items.map(function (item) {
        return '<button type="button" role="menuitem" data-msg-action="' + item.action + '"' +
          (item.danger ? ' class="is-danger"' : "") + ">" + item.icon + item.label + "</button>";
      }).join("");
      msgMenuEl.hidden = false;
      positionMenu(msgMenuEl, btn);
      msgMenuEl.querySelectorAll("[data-msg-action]").forEach(function (itemBtn) {
        itemBtn.addEventListener("click", function () {
          runMsgAction(itemBtn.getAttribute("data-msg-action"), payload);
        });
      });
    }

    function runMsgAction(action, payload) {
      closeMsgMenu();
      if (action === "pin") pinMessage(payload.el, !payload.pinned);
      else if (action === "forward") openChatDialog("forward", payload);
      else if (action === "task") openChatDialog("task", payload);
      else if (action === "recall") openChatDialog("confirm", payload, "recall");
      else if (action === "delete") openChatDialog("confirm", payload, "delete");
    }

    function setDialogSubmit(label, className, icon) {
      var submit = document.getElementById("chat-dialog-submit");
      if (!submit) return;
      submit.className = className || "btn btn-primary";
      submit.innerHTML = (icon || ICO.check) + '<span class="btn-label">' + label + "</span>";
    }

    function closeChatDialog() {
      if (!chatDialog) return;
      chatDialog.classList.remove("is-open");
      chatDialog.hidden = true;
      document.body.classList.remove("modal-open");
      setDialogSubmit("Xác nhận", "btn btn-primary", ICO.check);
      dialogState = { view: "", payload: null, target: "", kind: "" };
    }

    function fillForwardDialog(payload) {
      var preview = document.getElementById("forward-preview");
      var list = document.getElementById("forward-list");
      var note = document.getElementById("forward-note");
      if (note) note.value = "";
      if (preview) preview.textContent = payload ? payload.preview : "";
      if (!list) return;
      list.innerHTML = "";
      dialogState.target = "";
      document.querySelectorAll("#chat-list-chats [data-chat]").forEach(function (item) {
        var id = item.getAttribute("data-chat");
        var btn = document.createElement("button");
        btn.type = "button";
        btn.className = "forward-option";
        btn.setAttribute("role", "option");
        btn.setAttribute("data-target", id);
        btn.innerHTML =
          '<span class="chat-avatar' + (item.getAttribute("data-kind") === "group" ? " is-group" : "") + '">' +
          esc(item.getAttribute("data-initials") || "") + "</span><span>" +
          esc(item.getAttribute("data-name") || "") + "</span>";
        btn.addEventListener("click", function () {
          list.querySelectorAll(".forward-option").forEach(function (el) {
            el.classList.toggle("is-selected", el === btn);
          });
          dialogState.target = id;
        });
        list.appendChild(btn);
      });
    }

    function fillTaskDialog(payload) {
      var title = document.getElementById("task-title");
      var assignee = document.getElementById("task-assignee");
      var note = document.getElementById("task-note");
      var due = document.getElementById("task-due");
      var seed = "";
      if (payload && payload.type === "text") seed = (payload.text || "").replace(/^\[.*?\]\s*/, "");
      else if (payload && (payload.file || payload.text)) seed = payload.file || payload.text;
      if (title) title.value = seed;
      if (note) note.value = "";
      if (due && !due.value) due.value = "2026-09-12";
      if (!assignee) return;
      assignee.innerHTML = '<option value="">Chọn người thực hiện</option>';
      var meta = chatMeta[activeChat] || { members: [] };
      meta.members.forEach(function (member) {
        if (member.you) return;
        var opt = document.createElement("option");
        opt.value = member.name;
        opt.textContent = member.name;
        assignee.appendChild(opt);
      });
    }

    function openChatDialog(view, payload, kind) {
      closeMsgMenu();
      if (!chatDialog) return;
      dialogState = { view: view, payload: payload || null, target: "", kind: kind || "" };
      chatDialog.querySelectorAll("[data-dialog-view]").forEach(function (pane) {
        pane.hidden = pane.getAttribute("data-dialog-view") !== view;
      });
      var title = document.getElementById("chat-dialog-title");
      var submit = document.getElementById("chat-dialog-submit");
      setDialogSubmit("Xác nhận", "btn btn-primary", ICO.check);
      if (view === "forward") {
        if (title) title.textContent = "Chuyển tiếp";
        setDialogSubmit("Chuyển tiếp", "btn btn-primary", ICO.forward);
        fillForwardDialog(payload);
      } else if (view === "task") {
        if (title) title.textContent = "Giao việc";
        setDialogSubmit("Giao việc", "btn btn-primary", ICO.task);
        fillTaskDialog(payload);
      } else if (view === "confirm") {
        if (title) title.textContent = kind === "recall" ? "Thu hồi tin nhắn" : "Xóa tin nhắn";
        var copy = document.getElementById("confirm-copy");
        if (copy) copy.textContent = kind === "recall" ? t("chatRecallCopy") : t("chatDeleteCopy");
        if (kind === "recall") setDialogSubmit("Thu hồi", "btn btn-danger", ICO.recall);
        else setDialogSubmit("Xóa", "btn btn-danger", ICO.trash);
      }
      chatDialog.hidden = false;
      chatDialog.classList.add("is-open");
      document.body.classList.add("modal-open");
      var focusRoot = chatDialog.querySelector('[data-dialog-view]:not([hidden])');
      var focusEl = focusRoot && focusRoot.querySelector("input, select, .forward-option");
      if (!focusEl && view === "confirm") focusEl = submit;
      (focusEl || chatDialog.querySelector(".chat-dialog") || submit).focus();
    }

    function submitForward() {
      var target = dialogState.target;
      var payload = dialogState.payload;
      if (!target || !payload) {
        showToast(t("chatNoTarget"));
        return;
      }
      var note = ((document.getElementById("forward-note") || {}).value || "").trim();
      var inner = '<span class="msg-fwd-label">Đã chuyển tiếp</span>';
      var bubbleClass = "";
      var attrs = { type: payload.type || "text" };
      if (payload.type === "image") {
        bubbleClass = "msg-media";
        attrs.file = payload.file;
        attrs.tone = payload.tone || "teal";
        inner +=
          '<span class="msg-media-card chat-thumb-img is-' + esc(attrs.tone) + '" aria-hidden="true">' +
          '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><rect x="3" y="5" width="18" height="14" rx="2"/><circle cx="8.5" cy="10" r="1.5"/><path d="m21 15-4.5-4.5L7 20"/></svg>' +
          '</span><span class="msg-media-caption">' + esc(payload.file || "Hình ảnh") + "</span>";
      } else if (payload.type === "file") {
        bubbleClass = "msg-file";
        attrs.file = payload.file;
        attrs.ext = payload.ext || "FILE";
        inner +=
          '<span class="chat-thumb-ext is-' + esc(String(attrs.ext).toLowerCase()) + '">' + esc(attrs.ext) + "</span>" +
          '<span class="msg-file-name">' + esc(payload.file || "Tệp") + "</span>";
      } else {
        inner += '<blockquote class="msg-quote">' + esc(payload.text || payload.preview || "") + "</blockquote>";
      }
      if (note) inner += "<span>" + esc(note) + "</span>";
      appendOutgoing(target, attrs, inner, bubbleClass);
      rememberMedia(target, payload);
      closeChatDialog();
      showToast(t("chatForwarded"));
    }

    function submitTask() {
      var title = ((document.getElementById("task-title") || {}).value || "").trim();
      var assignee = ((document.getElementById("task-assignee") || {}).value || "").trim();
      var due = ((document.getElementById("task-due") || {}).value || "").trim();
      var note = ((document.getElementById("task-note") || {}).value || "").trim();
      if (!title || !assignee) {
        showToast(t("chatTaskNeed"));
        return;
      }
      var dueLabel = due ? due.split("-").reverse().join("/") : "Chưa đặt hạn";
      var inner =
        '<span class="msg-task-label">Giao việc</span>' +
        '<div class="msg-task-card"><strong>' + esc(title) + "</strong>" +
        "<p>Người thực hiện: " + esc(assignee) + "</p>" +
        "<p>Hạn: " + esc(dueLabel) + "</p>" +
        (note ? "<p>" + esc(note) + "</p>" : "") +
        "</div>";
      appendOutgoing(activeChat, { type: "task" }, inner, "");
      closeChatDialog();
      showToast(t("chatTaskCreated"));
    }

    function submitChatDialog() {
      if (dialogState.view === "forward") submitForward();
      else if (dialogState.view === "task") submitTask();
      else if (dialogState.view === "confirm" && dialogState.payload && dialogState.payload.el) {
        if (dialogState.kind === "recall") recallMessage(dialogState.payload.el);
        else deleteMessage(dialogState.payload.el);
        closeChatDialog();
      }
    }

    function fillMediaPanel(id, items, emptyText, kind) {
      var panel = document.getElementById(id);
      if (!panel) return;
      panel.innerHTML = "";
      if (!items.length) {
        var empty = document.createElement("p");
        empty.className = "chat-media-empty";
        empty.textContent = emptyText;
        panel.appendChild(empty);
        return;
      }
      items.forEach(function (item) {
        var btn = document.createElement("button");
        btn.type = "button";
        btn.setAttribute("aria-label", item.name);
        if (kind === "photos") {
          btn.className = "chat-thumb";
          var tone = item.tone || "teal";
          btn.innerHTML =
            '<span class="chat-thumb-img is-' + tone + '" aria-hidden="true">' +
            '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"><rect x="3" y="5" width="18" height="14" rx="2"/><circle cx="8.5" cy="10" r="1.5"/><path d="m21 15-4.5-4.5L7 20"/></svg>' +
            '</span><span class="sr-only">' + item.name + "</span>";
        } else {
          var ext = (item.ext || "FILE").toUpperCase();
          btn.className = "chat-thumb chat-thumb-file";
          btn.innerHTML =
            '<span class="chat-thumb-ext is-' + ext.toLowerCase() + '">' + ext + "</span>" +
            '<span class="chat-thumb-name">' + item.name + "</span>";
        }
        btn.addEventListener("click", function () {
          var pane = document.querySelector('[data-chat-pane="' + activeChat + '"]');
          var found = null;
          if (pane) {
            pane.querySelectorAll("[data-file]").forEach(function (msg) {
              if (msg.getAttribute("data-file") === item.name) found = msg;
            });
          }
          var payload = found ? msgPayload(found) : {
            el: null,
            type: kind === "photos" ? "image" : "file",
            file: item.name,
            ext: item.ext || "FILE",
            tone: item.tone || "teal",
            text: item.name,
            preview: (kind === "photos" ? "[Hình ảnh] " : "[Tệp] ") + item.name,
            isOut: false,
            recalled: false,
            pinned: false
          };
          openMsgMenu(btn, payload);
        });
        panel.appendChild(btn);
      });
    }

    function showMediaTab(name) {
      document.querySelectorAll(".chat-media-tab").forEach(function (tab) {
        var on = tab.getAttribute("data-media") === name;
        tab.classList.toggle("is-active", on);
        tab.setAttribute("aria-selected", on ? "true" : "false");
      });
      var photos = document.getElementById("chat-media-photos");
      var files = document.getElementById("chat-media-files");
      if (photos) photos.hidden = name !== "photos";
      if (files) files.hidden = name !== "files";
    }

    function fillChatInfo() {
      var item = document.querySelector('#chat-list-chats [data-chat="' + activeChat + '"]');
      var meta = chatMeta[activeChat] || { kindLabel: "Trao đổi riêng", members: [], photos: [], files: [] };
      var name = (item && item.getAttribute("data-name")) || "";
      var initials = (item && item.getAttribute("data-initials")) || "";
      var kind = (item && item.getAttribute("data-kind")) || "dm";
      var title = document.getElementById("chat-info-title");
      var kindEl = document.getElementById("chat-info-kind");
      var avatar = document.getElementById("chat-info-avatar");
      var members = document.getElementById("chat-info-members");
      cancelRename();
      if (title) title.textContent = name;
      if (kindEl) kindEl.textContent = meta.kindLabel;
      if (avatar) {
        avatar.classList.toggle("is-group", kind === "group");
        avatar.innerHTML = initials + (kind === "group" ? "" : '<span class="chat-dot is-offline" aria-hidden="true"></span>');
      }
      if (members) {
        members.innerHTML = "";
        var canManage = kind === "group" && meta.members.some(function (m) {
          return m.you && m.role === "Quản trị viên";
        });
        meta.members.forEach(function (member) {
          var li = document.createElement("li");
          li.className = "chat-member";
          var canRemove = canManage && !member.you && member.role !== "Quản trị viên";
          li.innerHTML =
            '<span class="chat-avatar">' + esc(member.initials) + "</span>" +
            '<span class="chat-member-meta"><strong>' + esc(member.name) + "</strong><span>" + esc(member.role) + "</span></span>" +
            (canRemove
              ? '<button class="doc-action is-danger" type="button" data-remove-member="' + esc(member.name) + '" aria-label="Xóa ' + esc(member.name) + ' khỏi nhóm">' + ICO.trash + "</button>"
              : "");
          members.appendChild(li);
        });
      }
      fillMediaPanel("chat-media-photos", meta.photos || [], t("chatNoPhotos"), "photos");
      fillMediaPanel("chat-media-files", meta.files || [], t("chatNoFiles"), "files");
      showMediaTab("photos");
      syncPinUI();
    }

    function removeGroupMember(name) {
      var item = document.querySelector('#chat-list-chats [data-chat="' + activeChat + '"]');
      if (!item || item.getAttribute("data-kind") !== "group") return;
      var meta = ensureMeta(activeChat);
      var canManage = meta.members.some(function (m) {
        return m.you && m.role === "Quản trị viên";
      });
      if (!canManage) return;
      meta.members = meta.members.filter(function (m) {
        return m.you || m.name !== name;
      });
      item.setAttribute("data-status", "Chat nhóm · " + meta.members.length + " thành viên");
      var headStatus = document.getElementById("chat-head-status");
      if (headStatus) {
        headStatus.innerHTML = '<span class="chat-status-dot" aria-hidden="true"></span> ' + item.getAttribute("data-status");
      }
      fillChatInfo();
      showToast(t("chatMemberRemoved"));
    }

    var membersList = document.getElementById("chat-info-members");
    if (membersList) {
      membersList.addEventListener("click", function (event) {
        var btn = event.target.closest("[data-remove-member]");
        if (!btn) return;
        removeGroupMember(btn.getAttribute("data-remove-member"));
      });
    }

    function openChatInfo() {
      closeMsgMenu();
      fillChatInfo();
      if (chatInfo) chatInfo.hidden = false;
      chatShell.classList.add("is-info-open");
      if (moreBtn) {
        moreBtn.classList.add("is-active");
        moreBtn.setAttribute("aria-expanded", "true");
      }
      var closeBtn = document.getElementById("chat-info-close");
      if (closeBtn) closeBtn.focus();
    }

    function setChatNav(kind) {
      var dm = document.getElementById("nav-chat-dm");
      var group = document.getElementById("nav-chat-group");
      if (dm) {
        dm.classList.toggle("is-current", kind !== "group");
        if (kind !== "group") dm.setAttribute("aria-current", "page");
        else dm.removeAttribute("aria-current");
      }
      if (group) {
        group.classList.toggle("is-current", kind === "group");
        if (kind === "group") group.setAttribute("aria-current", "page");
        else group.removeAttribute("aria-current");
      }
    }

    function showListTab(name) {
      document.querySelectorAll("[data-chat-list]").forEach(function (tab) {
        var on = tab.getAttribute("data-chat-list") === name;
        tab.classList.toggle("is-active", on);
        tab.setAttribute("aria-selected", on ? "true" : "false");
      });
      var chats = document.getElementById("chat-list-chats");
      var contacts = document.getElementById("chat-list-contacts");
      if (chats) chats.hidden = name !== "chats";
      if (contacts) contacts.hidden = name !== "contacts";
    }

    function openChat(id, fromHash) {
      var item = document.querySelector('#chat-list-chats [data-chat="' + id + '"]');
      if (!item) return;
      closeMsgMenu();
      activeChat = id;
      document.querySelectorAll("#chat-list-chats [data-chat]").forEach(function (el) {
        el.classList.toggle("is-active", el === item);
      });
      document.querySelectorAll("[data-chat-pane]").forEach(function (pane) {
        var on = pane.getAttribute("data-chat-pane") === id;
        pane.hidden = !on;
        pane.classList.toggle("is-active", on);
      });

      var name = item.getAttribute("data-name") || "";
      var status = item.getAttribute("data-status") || "";
      var initials = item.getAttribute("data-initials") || "";
      var kind = item.getAttribute("data-kind") || "dm";
      var headName = document.getElementById("chat-head-name");
      var headStatus = document.getElementById("chat-head-status");
      var headAvatar = document.getElementById("chat-head-avatar");
      if (headName) headName.textContent = name;
      if (headStatus) {
        headStatus.innerHTML = '<span class="chat-status-dot" aria-hidden="true"></span> ' + status;
      }
      if (headAvatar) {
        headAvatar.classList.toggle("is-group", kind === "group");
        headAvatar.innerHTML = initials + (kind === "group" ? "" : '<span class="chat-dot is-offline" aria-hidden="true"></span>');
      }
      setChatNav(kind);
      chatShell.classList.add("is-thread-open");
      syncPinUI();
      if (chatShell.classList.contains("is-info-open")) fillChatInfo();
      showListTab("chats");
      if (!fromHash) {
        if (kind === "group") {
          if (location.hash !== "#nhom") history.replaceState(null, "", "#nhom");
        } else if (location.hash === "#nhom") {
          history.replaceState(null, "", location.pathname);
        }
      }
    }

    document.querySelectorAll("[data-chat-list]").forEach(function (tab) {
      tab.addEventListener("click", function () {
        showListTab(tab.getAttribute("data-chat-list"));
      });
    });

    document.querySelectorAll("#chat-list-chats [data-chat]").forEach(function (item) {
      item.addEventListener("click", function () {
        openChat(item.getAttribute("data-chat"));
      });
    });

    function selectedContacts() {
      return Array.prototype.map.call(document.querySelectorAll("#chat-contact-list input:checked"), function (input) {
        return {
          id: input.value,
          name: input.getAttribute("data-name") || input.value,
          handle: input.getAttribute("data-handle") || "",
          initials: input.getAttribute("data-initials") || "?",
          chat: input.getAttribute("data-chat") || ""
        };
      });
    }

    function addChatPane(id) {
      var compose = document.getElementById("chat-compose");
      if (!compose) return;
      var pane = document.createElement("div");
      pane.className = "chat-msgs";
      pane.setAttribute("data-chat-pane", id);
      pane.setAttribute("aria-live", "polite");
      pane.hidden = true;
      pane.innerHTML = '<p class="chat-empty">Chưa có tin nhắn</p>';
      compose.parentNode.insertBefore(pane, compose);
    }

    function addChatItem(opt) {
      var list = document.getElementById("chat-list-chats");
      if (!list) return;
      var btn = document.createElement("button");
      btn.className = "chat-item";
      btn.type = "button";
      btn.setAttribute("data-chat", opt.id);
      btn.setAttribute("data-kind", opt.kind);
      btn.setAttribute("data-name", opt.name);
      btn.setAttribute("data-status", opt.status);
      btn.setAttribute("data-initials", opt.initials);
      if (opt.contact) btn.setAttribute("data-contact", opt.contact);
      var avatarClass = opt.kind === "group" ? "chat-avatar is-group" : "chat-avatar";
      var dot = opt.kind === "group" ? "" : '<span class="chat-dot is-offline" aria-hidden="true"></span>';
      btn.innerHTML =
        '<span class="' + avatarClass + '">' + esc(opt.initials) + dot + "</span>" +
        '<span class="chat-item-body">' +
          '<span class="chat-item-top">' +
            '<span class="chat-item-name">' + esc(opt.name) + "</span>" +
            '<span class="chat-item-end"><span class="chat-pin-mark" hidden aria-hidden="true"></span></span>' +
          "</span>" +
          '<span class="chat-item-preview">Chưa có tin nhắn</span>' +
        "</span>";
      btn.addEventListener("click", function () {
        openChat(opt.id);
      });
      list.insertBefore(btn, list.firstChild);
    }

    function syncGroupNameField() {
      var wrap = document.getElementById("chat-group-name-wrap");
      var input = document.getElementById("chat-group-name");
      var isGroup = selectedContacts().length > 1;
      var wasHidden = !wrap || wrap.hidden;
      if (wrap) wrap.hidden = !isGroup;
      if (!isGroup && input) input.value = "";
      if (isGroup && wasHidden && input) input.focus();
    }

    function resetContactForm() {
      document.querySelectorAll("#chat-contact-list input").forEach(function (input) {
        input.checked = false;
      });
      var groupName = document.getElementById("chat-group-name");
      if (groupName) groupName.value = "";
      syncGroupNameField();
    }

    function createConversation() {
      var picked = selectedContacts();
      if (!picked.length) {
        showToast(t("chatNeedContact"));
        return;
      }
      if (picked.length === 1) {
        var person = picked[0];
        var existing = (person.chat && document.querySelector('#chat-list-chats [data-chat="' + person.chat + '"]'))
          || document.querySelector('#chat-list-chats [data-contact="' + person.id + '"]');
        if (existing) {
          resetContactForm();
          openChat(existing.getAttribute("data-chat"));
          return;
        }
        var dmId = "dm-" + person.id;
        addChatPane(dmId);
        addChatItem({
          id: dmId,
          kind: "dm",
          name: person.name,
          status: "Ngoại tuyến · Trao đổi riêng",
          initials: person.initials,
          contact: person.id
        });
        var dmBox = document.querySelector('#chat-contact-list input[value="' + person.id + '"]');
        if (dmBox) dmBox.setAttribute("data-chat", dmId);
        chatMeta[dmId] = {
          kindLabel: "Trao đổi riêng",
          members: [
            { initials: person.initials, name: person.name, role: "Thành viên" },
            { initials: "B", name: "Bạn", role: "Quản trị viên", you: true }
          ],
          photos: [],
          files: []
        };
        resetContactForm();
        openChat(dmId);
        showToast(t("chatCreatedDm"));
        return;
      }
      var nameInput = document.getElementById("chat-group-name");
      var groupName = ((nameInput && nameInput.value) || "").trim();
      if (!groupName) {
        showToast(t("chatNeedGroupName"));
        if (nameInput) nameInput.focus();
        return;
      }
      var groupId = "g-" + Date.now().toString(36);
      var initials = groupName.replace(/\s+/g, "").slice(0, 2).toUpperCase() || "NH";
      addChatPane(groupId);
      addChatItem({
        id: groupId,
        kind: "group",
        name: groupName,
        status: "Chat nhóm · " + (picked.length + 1) + " thành viên",
        initials: initials
      });
      chatMeta[groupId] = {
        kindLabel: "Chat nhóm",
        members: picked.map(function (person) {
          var isAdmin = person.id === "admin" || person.name === "admin";
          return {
            initials: person.initials,
            name: person.name,
            role: isAdmin ? "Quản trị viên" : "Thành viên"
          };
        }).concat([{ initials: "B", name: "Bạn", role: "Quản trị viên", you: true }]),
        photos: [],
        files: []
      };
      resetContactForm();
      openChat(groupId);
      showToast(t("chatCreatedGroup"));
    }

    var createConv = document.getElementById("chat-create-conv");
    if (createConv) createConv.addEventListener("click", createConversation);
    var groupNameInput = document.getElementById("chat-group-name");
    if (groupNameInput) {
      groupNameInput.addEventListener("keydown", function (event) {
        if (event.key === "Enter") {
          event.preventDefault();
          createConversation();
        }
      });
    }
    var contactList = document.getElementById("chat-contact-list");
    if (contactList) {
      contactList.addEventListener("change", syncGroupNameField);
    }
    syncGroupNameField();

    var back = document.getElementById("chat-back");
    if (back) {
      back.addEventListener("click", function () {
        chatShell.classList.remove("is-thread-open");
        closeChatInfo();
      });
    }

    var refresh = document.getElementById("chat-refresh");
    if (refresh) {
      refresh.addEventListener("click", function () {
        showToast(t("chatRefreshed"));
      });
    }

    function filterChatLists() {
      var top = document.getElementById("chat-search");
      var local = document.getElementById("chat-contact-search");
      var qChats = ((top && top.value) || "").trim().toLowerCase();
      var qContacts = ((local && local.value) || (top && top.value) || "").trim().toLowerCase();
      document.querySelectorAll("#chat-list-chats .chat-item").forEach(function (item) {
        var text = (item.textContent || "").toLowerCase();
        item.hidden = !!(qChats && text.indexOf(qChats) === -1);
      });
      document.querySelectorAll("#chat-contact-list .chat-contact").forEach(function (row) {
        var text = (row.textContent || "").toLowerCase();
        row.hidden = !!(qContacts && text.indexOf(qContacts) === -1);
      });
    }

    var search = document.getElementById("chat-search");
    if (search) search.addEventListener("input", filterChatLists);
    var contactSearch = document.getElementById("chat-contact-search");
    if (contactSearch) contactSearch.addEventListener("input", filterChatLists);

    if (moreBtn) {
      moreBtn.addEventListener("click", function (event) {
        event.stopPropagation();
        if (chatShell.classList.contains("is-info-open")) closeChatInfo();
        else openChatInfo();
      });
    }

    var infoClose = document.getElementById("chat-info-close");
    if (infoClose) {
      infoClose.addEventListener("click", function () {
        closeChatInfo();
        moreBtn && moreBtn.focus();
      });
    }

    var renameStart = document.getElementById("chat-rename-start");
    var renameInput = document.getElementById("chat-rename");

    function currentChatName() {
      var item = document.querySelector('#chat-list-chats [data-chat="' + activeChat + '"]');
      return (item && item.getAttribute("data-name")) || "";
    }

    function startRename() {
      if (!renameInput || !renameStart) return;
      renameStart.hidden = true;
      renameInput.hidden = false;
      renameInput.value = currentChatName();
      renameInput.focus();
      renameInput.select();
    }

    function applyRename() {
      if (!renameInput) return;
      var next = renameInput.value.trim();
      if (!next) {
        renameInput.focus();
        return;
      }
      var item = document.querySelector('#chat-list-chats [data-chat="' + activeChat + '"]');
      if (item) {
        item.setAttribute("data-name", next);
        var nameEl = item.querySelector(".chat-item-name");
        if (nameEl) nameEl.textContent = next;
      }
      var headName = document.getElementById("chat-head-name");
      var infoTitle = document.getElementById("chat-info-title");
      if (headName) headName.textContent = next;
      if (infoTitle) infoTitle.textContent = next;
      cancelRename();
      showToast(t("chatRenamed"));
    }

    if (renameStart) renameStart.addEventListener("click", startRename);
    if (renameInput) {
      renameInput.addEventListener("keydown", function (event) {
        if (event.key === "Enter") {
          event.preventDefault();
          applyRename();
        } else if (event.key === "Escape") {
          event.preventDefault();
          cancelRename();
        }
      });
      renameInput.addEventListener("blur", function () {
        if (renameInput.hidden) return;
        var next = renameInput.value.trim();
        if (!next || next === currentChatName()) {
          cancelRename();
          return;
        }
        applyRename();
      });
    }

    document.querySelectorAll(".chat-media-tab").forEach(function (btn) {
      btn.addEventListener("click", function () {
        showMediaTab(btn.getAttribute("data-media"));
      });
    });

    var compose = document.getElementById("chat-compose");
    var input = document.getElementById("chat-input");
    var sendBtn = compose && compose.querySelector(".chat-send");
    if (input && sendBtn) {
      input.addEventListener("input", function () {
        sendBtn.disabled = !input.value.trim();
      });
    }
    if (compose && input) {
      compose.addEventListener("submit", function (event) {
        event.preventDefault();
        var text = input.value.trim();
        if (!text) {
          showToast(t("chatEmpty"));
          return;
        }
        appendOutgoing(activeChat, { type: "text" }, esc(text), "");
        input.value = "";
        if (sendBtn) sendBtn.disabled = true;
        input.focus();
      });
    }

    chatShell.addEventListener("click", function (event) {
      var more = event.target.closest(".msg-more");
      if (!more || !chatShell.contains(more)) return;
      event.stopPropagation();
      var msg = more.closest(".msg");
      if (msg) openMsgMenu(more, msgPayload(msg));
    });

    var pinConvBtn = document.getElementById("chat-pin-conv");
    if (pinConvBtn) pinConvBtn.addEventListener("click", togglePinConv);
    var infoPinBtn = document.getElementById("chat-info-pin");
    if (infoPinBtn) infoPinBtn.addEventListener("click", togglePinConv);
    var pinJump = document.getElementById("chat-pin-jump");
    if (pinJump) pinJump.addEventListener("click", jumpToPinned);
    var pinClear = document.getElementById("chat-pin-clear");
    if (pinClear) pinClear.addEventListener("click", unpinCurrent);

    if (chatDialog) {
      chatDialog.addEventListener("click", function (event) {
        if (event.target === chatDialog) closeChatDialog();
      });
      chatDialog.querySelectorAll("[data-chat-dialog-close]").forEach(function (btn) {
        btn.addEventListener("click", closeChatDialog);
      });
      var dialogSubmit = document.getElementById("chat-dialog-submit");
      if (dialogSubmit) dialogSubmit.addEventListener("click", submitChatDialog);
      chatDialog.addEventListener("keydown", function (event) {
        if (event.key !== "Enter" || event.target.tagName === "TEXTAREA" || event.target.tagName === "BUTTON") return;
        event.preventDefault();
        submitChatDialog();
      });
    }

    window.addEventListener("resize", closeMsgMenu);

    function openFromHash() {
      if (location.hash === "#nhom") openChat("nhom", true);
    }
    openFromHash();
    syncPinUI();
    window.addEventListener("hashchange", openFromHash);
  }

  var socialApp = document.getElementById("social-app");
  if (socialApp) {
    var wallFilter = "all";
    var searchMode = "posts";
    var socialQuery = "";
    var socialTag = "";
    var REACTS = [
      { id: "like", label: "Thích", icon: '<svg viewBox="0 0 24 24" aria-hidden="true"><path fill="#1877F2" d="M10.2 8.2 11 3.8A2.2 2.2 0 0 1 13.2 2c.8 0 1.3.6 1.3 1.6V8h4.6a2 2 0 0 1 2 2.3l-1.1 7.2A2.3 2.3 0 0 1 17.8 20H9.5V9.2l.7-1z"/><path fill="#1877F2" d="M4 10h3.2v10H5.2A1.2 1.2 0 0 1 4 18.8V10z"/></svg>' },
      { id: "love", label: "Yêu thích", icon: '<svg viewBox="0 0 24 24" aria-hidden="true"><path fill="#E11D48" d="M12 21S4 15.8 2.6 11.6C1.4 8 3.2 5 6.4 5c1.8 0 3.2 1 3.8 2.3C10.8 6 12.2 5 14 5c3.2 0 5 3 3.8 6.6C16 15.8 12 21 12 21z"/></svg>' },
      { id: "haha", label: "Haha", icon: '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="10" fill="#F5B400"/><path d="M6.6 9.6c1.3-1.6 2.8-1.6 3.6 0" fill="none" stroke="#5C3D00" stroke-width="1.6" stroke-linecap="round"/><path d="M13.8 9.6c1.3-1.6 2.8-1.6 3.6 0" fill="none" stroke="#5C3D00" stroke-width="1.6" stroke-linecap="round"/><ellipse cx="12" cy="15.1" rx="3.4" ry="2.5" fill="#5C3D00"/><path fill="#E11D48" d="M10.4 15.6c.5 1.2 1.1 1.7 1.6 1.7s1.1-.5 1.6-1.7c-.5.2-1 .3-1.6.3s-1.1-.1-1.6-.3z"/></svg>' },
      { id: "smile", label: "Vui", icon: '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="10" fill="#F5B400"/><circle cx="8.4" cy="10" r="1.35" fill="#5C3D00"/><circle cx="15.6" cy="10" r="1.35" fill="#5C3D00"/><path d="M8.2 14c1.3 1.8 3 2.6 3.8 2.6s2.5-.8 3.8-2.6" fill="none" stroke="#5C3D00" stroke-width="1.7" stroke-linecap="round"/></svg>' },
      { id: "clap", label: "Vỗ tay", icon: '<svg viewBox="0 0 24 24" fill="none" stroke="#EA580C" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M8 13 5 8l2-2 4 5"/><path d="m11 11 2-5 2 1-1 5"/><path d="M9 16c0 3 2 5 5 5h1a4 4 0 0 0 4-4v-5l-3-1-2 4"/></svg>' },
      { id: "think", label: "Suy nghĩ", icon: '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="10" fill="#60A5FA"/><circle cx="8.4" cy="10" r="1.35" fill="#1E3A8A"/><circle cx="15.6" cy="10" r="1.35" fill="#1E3A8A"/><path d="M9.2 15.4c.8-.7 1.8-1 2.8-1s2 .3 2.8 1" fill="none" stroke="#1E3A8A" stroke-width="1.6" stroke-linecap="round"/></svg>' },
      { id: "sad", label: "Buồn", icon: '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="10" fill="#F59E0B"/><circle cx="8.4" cy="10" r="1.35" fill="#5C3D00"/><circle cx="15.6" cy="10" r="1.35" fill="#5C3D00"/><path d="M15.8 16.2c-1.3-1.6-2.8-2.3-3.8-2.3s-2.5.7-3.8 2.3" fill="none" stroke="#5C3D00" stroke-width="1.7" stroke-linecap="round"/></svg>' }
    ];
    var people = [
      { initials: "TH", name: "Trần Việt Hùng", role: "@hungtv · Trang của bạn", href: "bang-tin.html#toi" },
      { initials: "NH", name: "nguyễn hồ phi long", role: "Chuyên viên · Chat nội bộ", href: "chat.html" },
      { initials: "PT", name: "Phó trưởng phòng", role: "Lãnh đạo · Chat nội bộ", href: "chat.html" }
    ];

    function htmlEsc(value) {
      return String(value == null ? "" : value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
    }

    function fillReactBar(bar) {
      if (!bar) return;
      bar.innerHTML = REACTS.map(function (item) {
        return '<button class="react-choice" type="button" role="option" data-react="' + item.id + '" aria-label="' + item.label + '" title="' + item.label + '" aria-pressed="false">' + item.icon + "</button>";
      }).join("");
    }

    function fillAllReactBars() {
      document.querySelectorAll(".react-bar").forEach(fillReactBar);
    }

    function ownReactWrap(root) {
      return root.classList.contains("comment")
        ? root.querySelector(".react-wrap")
        : root.querySelector(".post-actions .react-wrap");
    }

    function findReact(id) {
      return REACTS.filter(function (item) { return item.id === id; })[0];
    }

    function syncReactBar(root) {
      var type = root.getAttribute("data-my-react");
      var wrap = ownReactWrap(root);
      if (!wrap) return;
      wrap.classList.remove("is-open");
      wrap.querySelectorAll("[data-react]").forEach(function (btn) {
        var on = btn.getAttribute("data-react") === type;
        btn.classList.toggle("is-on", on);
        btn.setAttribute("aria-pressed", on ? "true" : "false");
      });
      var trigger = wrap.querySelector("[data-react-open]");
      if (!trigger) return;
      var found = findReact(type);
      var isPost = !root.classList.contains("comment");
      if (found) {
        trigger.innerHTML = found.icon + "<span>" + found.label + "</span>";
        trigger.className = (isPost ? "post-action" : "comment-react-open") + " is-on is-" + type;
      } else {
        trigger.textContent = "Cảm xúc";
        trigger.className = isPost ? "post-action" : "comment-react-open";
      }
    }

    function isProfile() {
      return location.hash === "#toi";
    }

    function updateSocialNav() {
      var feed = document.getElementById("nav-feed");
      var profile = document.getElementById("nav-profile");
      var onProfile = isProfile();
      if (feed) feed.classList.toggle("is-current", !onProfile);
      if (profile) profile.classList.toggle("is-current", onProfile);
    }

    function showSocialView() {
      var onProfile = isProfile();
      var profileView = document.getElementById("view-profile");
      var feedView = document.getElementById("view-feed");
      var asideProfile = document.getElementById("aside-profile");
      var asideFeed = document.getElementById("aside-feed");
      if (profileView) profileView.hidden = !onProfile;
      if (feedView) feedView.hidden = onProfile;
      if (asideProfile) asideProfile.hidden = !onProfile;
      if (asideFeed) asideFeed.hidden = onProfile;
      updateSocialNav();
      applySocialFilter();
    }

    function applySocialFilter() {
      var stream = document.getElementById("social-stream");
      var empty = document.getElementById("social-empty");
      var peopleBox = document.getElementById("people-results");
      var posts = document.querySelectorAll("#social-stream .post-card");
      var profile = isProfile();
      var visible = 0;
      var q = socialQuery.toLowerCase();
      var tag = socialTag.toLowerCase().replace(/\s+/g, "");
      if (tag && tag.charAt(0) !== "#") tag = "#" + tag;

      if (searchMode === "people") {
        if (stream) stream.hidden = true;
        if (empty) empty.hidden = true;
        renderPeople(q);
        if (peopleBox) peopleBox.hidden = false;
        return;
      }
      if (peopleBox) peopleBox.hidden = true;
      if (stream) stream.hidden = false;

      posts.forEach(function (post) {
        var wallOk = !profile || post.getAttribute("data-wall") === "me";
        var vis = post.getAttribute("data-visibility");
        var visOk = wallFilter === "all" || vis === wallFilter;
        var text = (post.getAttribute("data-text") || "").toLowerCase();
        var tags = (post.getAttribute("data-tags") || "").toLowerCase();
        var qOk = !q || text.indexOf(q) !== -1;
        var tagOk = !tag || tags.indexOf(tag) !== -1;
        var show = wallOk && visOk && qOk && tagOk;
        post.hidden = !show;
        if (show) visible += 1;
      });
      if (empty) empty.hidden = visible > 0;
    }

    function renderPeople(q) {
      var box = document.getElementById("people-results");
      if (!box) return;
      box.innerHTML = "";
      people.filter(function (person) {
        return !q || person.name.toLowerCase().indexOf(q) !== -1 || person.role.toLowerCase().indexOf(q) !== -1;
      }).forEach(function (person) {
        var card = document.createElement("a");
        card.className = "people-card";
        card.href = person.href;
        card.innerHTML =
          '<span class="chat-avatar">' + htmlEsc(person.initials) + "</span>" +
          "<span><strong>" + htmlEsc(person.name) + "</strong><span>" + htmlEsc(person.role) + "</span></span>";
        box.appendChild(card);
      });
      if (!box.children.length) {
        box.innerHTML = '<p class="social-empty">Không tìm thấy cá nhân phù hợp.</p>';
      }
    }

    function setReaction(root, type) {
      var current = root.getAttribute("data-my-react");
      var count = parseInt(root.getAttribute("data-react-count") || "0", 10);
      if (current === type) {
        root.removeAttribute("data-my-react");
        count = Math.max(0, count - 1);
      } else {
        if (!current) count += 1;
        root.setAttribute("data-my-react", type);
      }
      root.setAttribute("data-react-count", String(count));
      syncReactBar(root);
      var summary = root.querySelector(":scope > [data-react-summary], :scope > .post-stats [data-react-summary]");
      if (summary) summary.textContent = count + " cảm xúc";
    }

    function addComment(post, text) {
      var list = post.querySelector(".post-comments");
      if (!list) return;
      var form = list.querySelector(".comment-compose");
      var item = document.createElement("article");
      item.className = "comment";
      item.innerHTML =
        '<span class="chat-avatar sm">TH</span><div>' +
        '<div class="comment-bubble"><strong>Trần Việt Hùng</strong><p>' + htmlEsc(text) + "</p></div>" +
        '<div class="comment-meta"><time>Vừa xong</time> · <button type="button" data-reply="Trần Việt Hùng">Trả lời</button>' +
        '<span class="react-wrap"><button class="comment-react-open" type="button" data-react-open>Cảm xúc</button>' +
        '<div class="react-bar" role="listbox" aria-label="Chọn cảm xúc"></div></span></div></div>';
      fillReactBar(item.querySelector(".react-bar"));
      if (form) list.insertBefore(item, form);
      else list.appendChild(item);
      var count = parseInt(post.getAttribute("data-comment-count") || "0", 10) + 1;
      post.setAttribute("data-comment-count", String(count));
      post.querySelectorAll("[data-comment-toggle]").forEach(function (btn) {
        if (btn.closest(".post-stats")) btn.textContent = count + " bình luận";
      });
      list.hidden = false;
    }

    function composePost(text, vis) {
      var stream = document.getElementById("social-stream");
      if (!stream) return;
      var id = "p-" + Date.now().toString(36);
      var visLabel = vis === "public" ? "Công khai" : vis === "private" ? "Riêng tư" : "Nội bộ";
      var article = document.createElement("article");
      article.className = "post-card";
      article.setAttribute("data-post-id", id);
      article.setAttribute("data-wall", "me");
      article.setAttribute("data-visibility", vis);
      article.setAttribute("data-author", "me");
      article.setAttribute("data-text", text.toLowerCase());
      article.setAttribute("data-tags", "");
      article.setAttribute("data-react-count", "0");
      article.setAttribute("data-comment-count", "0");
      article.innerHTML =
        '<header class="post-head"><span class="chat-avatar">TH</span><div class="post-meta">' +
        '<a class="post-author" href="bang-tin.html#toi">Trần Việt Hùng</a>' +
        "<p><time>Vừa xong</time> · <span class=\"post-vis is-" + vis + "\">" + visLabel + "</span></p></div></header>" +
        '<div class="post-body"><p>' + htmlEsc(text) + "</p></div>" +
        '<div class="post-stats"><span data-react-summary>0 cảm xúc</span><button type="button" data-comment-toggle>0 bình luận</button></div>' +
        '<div class="post-actions"><div class="react-wrap"><button class="post-action" type="button" data-react-open>Cảm xúc</button>' +
        '<div class="react-bar" role="listbox" aria-label="Chọn cảm xúc"></div></div>' +
        '<button class="post-action" type="button" data-comment-toggle>Bình luận</button>' +
        '<button class="post-action" type="button" data-share>Chia sẻ</button></div>' +
        '<div class="post-comments" hidden><form class="comment-compose"><span class="chat-avatar sm">TH</span>' +
        '<label class="sr-only">Viết bình luận</label>' +
        '<input class="input" type="text" placeholder="Viết bình luận..." autocomplete="off">' +
        '<button class="btn btn-primary btn-sm" type="submit"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M22 2 11 13"/><path d="M22 2 15 22l-4-9-9-4z"/></svg><span>Gửi</span></button></form></div>';
      stream.insertBefore(article, stream.firstChild);
      fillReactBar(article.querySelector(".react-bar"));
      applySocialFilter();
    }

    fillAllReactBars();
    showSocialView();
    window.addEventListener("hashchange", showSocialView);

    document.querySelectorAll("[data-wall-filter]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        wallFilter = btn.getAttribute("data-wall-filter") || "all";
        document.querySelectorAll("[data-wall-filter]").forEach(function (el) {
          var on = el === btn;
          el.classList.toggle("is-active", on);
          el.setAttribute("aria-selected", on ? "true" : "false");
        });
        applySocialFilter();
      });
    });

    document.querySelectorAll("[data-search-mode]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        searchMode = btn.getAttribute("data-search-mode") || "posts";
        document.querySelectorAll("[data-search-mode]").forEach(function (el) {
          var on = el === btn;
          el.classList.toggle("is-active", on);
          el.setAttribute("aria-selected", on ? "true" : "false");
        });
        applySocialFilter();
      });
    });

    var searchForm = document.getElementById("social-search-form");
    var searchToggle = document.getElementById("social-search-toggle");

    function syncSearchToggle() {
      if (!searchToggle) return;
      var open = !!(searchForm && !searchForm.hidden);
      searchToggle.setAttribute("aria-expanded", open ? "true" : "false");
      searchToggle.setAttribute("aria-label", open ? "Đóng tìm kiếm" : "Tìm kiếm");
      searchToggle.classList.toggle("is-filtered", !!(socialQuery || socialTag));
    }

    function openSocialSearch(focus) {
      if (!searchForm) return;
      searchForm.hidden = false;
      syncSearchToggle();
      if (focus !== false) {
        var q = document.getElementById("social-q");
        if (q) q.focus();
      }
    }

    function closeSocialSearch() {
      if (!searchForm) return;
      searchForm.hidden = true;
      syncSearchToggle();
      if (searchToggle) searchToggle.focus();
    }

    if (searchToggle && searchForm) {
      searchToggle.addEventListener("click", function () {
        if (searchForm.hidden) openSocialSearch(true);
        else closeSocialSearch();
      });
      searchForm.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
          event.preventDefault();
          closeSocialSearch();
        }
      });
    }

    if (searchForm) {
      searchForm.addEventListener("submit", function (event) {
        event.preventDefault();
        socialQuery = ((document.getElementById("social-q") || {}).value || "").trim();
        socialTag = ((document.getElementById("social-tag") || {}).value || "").trim();
        applySocialFilter();
        syncSearchToggle();
        showToast(t("socialFiltered"));
      });
    }

    var clearBtn = document.getElementById("social-search-clear");
    if (clearBtn) {
      clearBtn.addEventListener("click", function () {
        var q = document.getElementById("social-q");
        var tag = document.getElementById("social-tag");
        if (q) q.value = "";
        if (tag) tag.value = "";
        socialQuery = "";
        socialTag = "";
        applySocialFilter();
        closeSocialSearch();
        showToast(t("socialCleared"));
      });
    }

    var visToggle = document.getElementById("social-compose-vis");
    if (visToggle) {
      visToggle.addEventListener("change", function () {
        var label = document.querySelector("[data-vis-label]");
        var mode = visToggle.checked ? "Nội bộ" : "Riêng tư";
        if (label) label.textContent = mode;
        visToggle.setAttribute("aria-label", "Phạm vi: " + mode);
      });
    }

    var compose = document.getElementById("social-compose");
    if (compose) {
      compose.addEventListener("submit", function (event) {
        event.preventDefault();
        var area = document.getElementById("social-compose-text");
        var vis = document.getElementById("social-compose-vis");
        var text = ((area && area.value) || "").trim();
        if (!text) {
          showToast(t("socialEmptyPost"));
          area && area.focus();
          return;
        }
        composePost(text, vis && vis.checked ? "internal" : "private");
        if (area) area.value = "";
        showToast(t("socialPosted"));
      });
    }

    socialApp.addEventListener("click", function (event) {
      var tagBtn = event.target.closest("[data-tag]");
      if (tagBtn) {
        var tagInput = document.getElementById("social-tag");
        socialTag = tagBtn.getAttribute("data-tag") || "";
        if (tagInput) tagInput.value = socialTag;
        searchMode = "posts";
        document.querySelectorAll("[data-search-mode]").forEach(function (el) {
          var on = el.getAttribute("data-search-mode") === "posts";
          el.classList.toggle("is-active", on);
          el.setAttribute("aria-selected", on ? "true" : "false");
        });
        openSocialSearch(false);
        applySocialFilter();
        syncSearchToggle();
        return;
      }

      var reactOpen = event.target.closest("[data-react-open]");
      if (reactOpen) {
        var wrap = reactOpen.closest(".react-wrap");
        if (wrap) {
          var noHover = window.matchMedia("(hover: none)").matches;
          var inComment = !!reactOpen.closest(".comment");
          if (noHover || inComment) {
            var willOpen = !wrap.classList.contains("is-open");
            document.querySelectorAll(".react-wrap.is-open").forEach(function (el) {
              el.classList.remove("is-open");
            });
            wrap.classList.toggle("is-open", willOpen);
          }
        }
        return;
      }

      var reactChoice = event.target.closest("[data-react]");
      if (reactChoice) {
        var root = reactChoice.closest(".comment") || reactChoice.closest(".post-card");
        if (root) setReaction(root, reactChoice.getAttribute("data-react"));
        return;
      }

      var commentToggle = event.target.closest("[data-comment-toggle]");
      if (commentToggle) {
        var postC = commentToggle.closest(".post-card");
        var box = postC && postC.querySelector(".post-comments");
        if (box) {
          box.hidden = !box.hidden;
          if (!box.hidden) {
            var input = box.querySelector("input");
            if (input) input.focus();
          }
        }
        return;
      }

      var share = event.target.closest("[data-share]");
      if (share) {
        var postS = share.closest(".post-card");
        var url = (location.href.split("#")[0].split("?")[0]) + "?post=" + ((postS && postS.getAttribute("data-post-id")) || "");
        if (navigator.clipboard && navigator.clipboard.writeText) {
          navigator.clipboard.writeText(url).catch(function () {});
        }
        showToast(t("socialShared"));
        return;
      }

      var reply = event.target.closest("[data-reply]");
      if (reply) {
        var postR = reply.closest(".post-card");
        var comments = postR && postR.querySelector(".post-comments");
        if (comments) comments.hidden = false;
        var inputR = comments && comments.querySelector("input");
        if (inputR) {
          inputR.value = "@" + reply.getAttribute("data-reply") + " ";
          inputR.focus();
        }
      }
    });

    socialApp.addEventListener("submit", function (event) {
      var form = event.target.closest(".comment-compose");
      if (!form) return;
      event.preventDefault();
      var input = form.querySelector("input");
      var text = ((input && input.value) || "").trim();
      var post = form.closest(".post-card");
      if (!text || !post) return;
      addComment(post, text);
      if (input) input.value = "";
      showToast(t("socialCommented"));
    });
  }

  var eventDash = document.getElementById("view-event-dash");
  var eventList = document.getElementById("view-event-list");
  var eventDetail = document.getElementById("view-event-detail");
  var eventModal = document.getElementById("event-modal");
  var guestModal = document.getElementById("guest-modal");
  if (eventDash || eventList || eventDetail) {
    var STATUS_LABEL = { preparing: "Đang chuẩn bị", live: "Đang diễn ra", soon: "Sắp diễn ra", done: "Đã tổ chức" };
    var STATUS_CLASS = { preparing: "badge-prep", live: "badge-live", soon: "badge-info", done: "badge-done" };
    var REG_LABEL = { confirmed: "Đã xác nhận", pending: "Chưa xác nhận", declined: "Từ chối" };
    var REG_CLASS = { confirmed: "badge-live", pending: "badge-warn", declined: "badge-done" };
    var CHECK_LABEL = { in: "Đã check-in", out: "Chưa check-in" };
    var CHECK_CLASS = { in: "badge-live", out: "badge-muted" };
    var EVENT_USERS = [
      { id: "hungtv", name: "Trần Việt Hùng", cccd: "079085001111", phone: "0901112233", email: "hungtv@hcs.vn", initials: "TH" },
      { id: "admin", name: "admin", cccd: "001082000001", phone: "0900000001", email: "admin@hcs.vn", initials: "A" },
      { id: "long", name: "Nguyễn Hồ Phi Long", cccd: "079090012345", phone: "0912345670", email: "long.nhp@hcs.vn", initials: "NL" },
      { id: "ptp", name: "Phó trưởng phòng", cccd: "001079000222", phone: "0902223344", email: "ptp@hcs.vn", initials: "PT" },
      { id: "trang", name: "Đặng Hiền Trang", cccd: "001095003333", phone: "0934567890", email: "trang.dh@hcs.vn", initials: "ĐT" },
      { id: "khoa", name: "Nguyễn Minh Khoa", cccd: "079201001234", phone: "0901234567", email: "khoa.nm@hcs.vn", initials: "NK" },
      { id: "ha", name: "Lê Thu Hà", cccd: "079201005678", phone: "0912345678", email: "ha.lt@hcs.vn", initials: "LH" }
    ];

    function eventRowById(id) {
      if (!id) return null;
      return document.querySelector('#event-body tr[data-event-id="' + id + '"]');
    }

    function escapeEventHtml(str) {
      return String(str || "").replace(/[&<>"']/g, function (ch) {
        return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
      });
    }

    function showEventView() {
      var hash = location.hash;
      var id = hash ? hash.replace(/^#/, "") : "";
      var detailRow = (id && id !== "danh-sach" && id !== "tao") ? eventRowById(id) : null;
      var isDetail = !!detailRow;
      var isList = hash === "#danh-sach" || hash === "#tao";
      if (eventDash) eventDash.hidden = isList || isDetail;
      if (eventList) eventList.hidden = !isList || isDetail;
      if (eventDetail) eventDetail.hidden = !isDetail;
      var dashNav = document.getElementById("nav-event-dash");
      var listNav = document.getElementById("nav-event-list");
      if (dashNav) dashNav.classList.toggle("is-current", !isList && !isDetail);
      if (listNav) listNav.classList.toggle("is-current", isList || isDetail);
      if (hash === "#tao") openEventModal(null);
      if (isDetail) fillEventDetail(detailRow);
    }

    function openEventModal(row) {
      if (!eventModal) return;
      var form = document.getElementById("event-form");
      var title = document.getElementById("event-modal-title");
      var saveLabel = document.querySelector("#event-save .btn-label");
      var creating = !row;
      if (title) title.textContent = creating ? "Tạo sự kiện mới" : "Chỉnh sửa sự kiện";
      if (saveLabel) saveLabel.textContent = creating ? "Tạo sự kiện" : "Lưu thay đổi";
      if (form) form.reset();
      document.getElementById("event-id").value = creating ? "" : row.getAttribute("data-event-id") || "";
      document.getElementById("event-group-input").value = creating ? "" : row.getAttribute("data-group") || "";
      document.getElementById("event-name").value = creating ? "" : row.getAttribute("data-name") || "";
      document.getElementById("event-start").value = creating ? "" : row.getAttribute("data-start") || "";
      document.getElementById("event-end").value = creating ? "" : row.getAttribute("data-end") || "";
      document.getElementById("event-place").value = creating ? "" : row.getAttribute("data-place") || "";
      document.getElementById("event-status-input").value = creating ? "preparing" : row.getAttribute("data-status") || "preparing";
      document.getElementById("event-content").value = creating ? "" : row.getAttribute("data-content") || "";
      document.getElementById("event-note").value = creating ? "" : row.getAttribute("data-note") || "";
      eventModal.hidden = false;
      eventModal.classList.add("is-open");
      document.body.classList.add("modal-open");
      var first = document.getElementById("event-group-input");
      if (first) first.focus();
    }

    function closeEventModal() {
      if (!eventModal) return;
      eventModal.classList.remove("is-open");
      eventModal.hidden = true;
      document.body.classList.remove("modal-open");
      if (location.hash === "#tao") history.replaceState(null, "", "su-kien.html#danh-sach");
    }

    function formatEventWhen(value) {
      if (!value) return "";
      var parts = value.split("T");
      if (parts.length < 2) return value;
      var d = parts[0].split("-");
      return d[2] + "/" + d[1] + "/" + d[0] + " " + parts[1];
    }

    function writeEventRow(row, data) {
      row.setAttribute("data-name", data.name);
      row.setAttribute("data-group", data.group);
      row.setAttribute("data-start", data.start);
      row.setAttribute("data-end", data.end);
      row.setAttribute("data-place", data.place);
      row.setAttribute("data-status", data.status);
      row.setAttribute("data-content", data.content || "");
      row.setAttribute("data-note", data.note || "");
      if (data.attendees != null) row.setAttribute("data-attendees", String(data.attendees));
      var nameEl = row.querySelector(".event-name");
      if (nameEl) nameEl.textContent = data.name;
      row.children[1].textContent = data.group;
      row.children[2].innerHTML = "<span>" + formatEventWhen(data.start) + "</span><span>" + formatEventWhen(data.end) + "</span>";
      row.children[3].textContent = data.place;
      row.children[4].innerHTML = '<span class="badge ' + (STATUS_CLASS[data.status] || "badge-info") + '">' + (STATUS_LABEL[data.status] || data.status) + "</span>";
      if (data.attendees != null) row.children[5].textContent = data.attendees;
    }

    function currentEventId() {
      return (document.getElementById("ed-id") || {}).value || "";
    }

    function eventGuestRows(id) {
      return [].filter.call(document.querySelectorAll("#ed-guest-body tr[data-guest]"), function (row) {
        return row.getAttribute("data-ed-for") === id && row.getAttribute("data-deleted") !== "1";
      });
    }

    function renderEventQr(text) {
      var mount = document.getElementById("ed-qr-mount");
      if (!mount) return;
      var size = 21;
      var cells = new Array(size * size);
      var i;
      for (i = 0; i < cells.length; i++) cells[i] = 0;
      function set(x, y, on) {
        if (x >= 0 && y >= 0 && x < size && y < size) cells[y * size + x] = on ? 1 : 0;
      }
      function finder(ox, oy) {
        var x;
        var y;
        for (y = 0; y < 7; y++) {
          for (x = 0; x < 7; x++) {
            var edge = x === 0 || x === 6 || y === 0 || y === 6;
            var core = x >= 2 && x <= 4 && y >= 2 && y <= 4;
            set(ox + x, oy + y, edge || core);
          }
        }
      }
      function reserved(x, y) {
        var tl = x < 7 && y < 7;
        var tr = x >= 14 && y < 7;
        var bl = x < 7 && y >= 14;
        return tl || tr || bl || x === 6 || y === 6;
      }
      finder(0, 0);
      finder(14, 0);
      finder(0, 14);
      for (i = 8; i <= 12; i++) {
        set(i, 6, i % 2 === 0);
        set(6, i, i % 2 === 0);
      }
      var hash = 2166136261;
      var str = String(text || "HCS");
      for (i = 0; i < str.length; i++) hash = (hash ^ str.charCodeAt(i)) * 16777619 >>> 0;
      var x;
      var y;
      for (y = 0; y < size; y++) {
        for (x = 0; x < size; x++) {
          if (reserved(x, y)) continue;
          hash = (hash * 1664525 + 1013904223) >>> 0;
          set(x, y, hash & 1);
        }
      }
      var parts = [];
      for (y = 0; y < size; y++) {
        for (x = 0; x < size; x++) {
          if (cells[y * size + x]) parts.push("M" + x + " " + y + "h1v1h-1z");
        }
      }
      mount.innerHTML = '<svg class="ed-qr-art" viewBox="-1 -1 23 23" aria-hidden="true"><rect x="-1" y="-1" width="23" height="23" fill="var(--color-white)"/><path d="' + parts.join("") + '" fill="currentColor"/></svg>';
    }

    function updateEventStats() {
      var id = currentEventId();
      var rows = eventGuestRows(id);
      var confirmed = 0;
      var pending = 0;
      var declined = 0;
      var checked = 0;
      rows.forEach(function (row) {
        var reg = row.getAttribute("data-reg");
        if (reg === "confirmed") confirmed += 1;
        else if (reg === "declined") declined += 1;
        else pending += 1;
        if (row.getAttribute("data-check") === "in") checked += 1;
      });
      var total = rows.length;
      var setText = function (eid, value) {
        var el = document.getElementById(eid);
        if (el) el.textContent = value;
      };
      setText("ed-stat-total", total);
      setText("ed-stat-confirmed", confirmed);
      setText("ed-stat-pending", pending);
      setText("ed-stat-declined", declined);
      setText("ed-stat-in", checked);
      setText("ed-stat-out", total - checked);
      var listRow = eventRowById(id);
      if (listRow) {
        listRow.setAttribute("data-attendees", String(total));
        listRow.children[5].textContent = total;
      }
    }

    function refreshDetailTables() {
      var id = currentEventId();
      var fileVisible = 0;
      document.querySelectorAll("#ed-file-body tr[data-file]").forEach(function (row) {
        var show = row.getAttribute("data-ed-for") === id && row.getAttribute("data-deleted") !== "1";
        row.hidden = !show;
        if (show) fileVisible += 1;
      });
      var fileEmpty = document.getElementById("ed-file-empty");
      if (fileEmpty) fileEmpty.hidden = fileVisible > 0;
      applyGuestFilter();
      updateEventStats();
    }

    function applyGuestFilter() {
      var id = currentEventId();
      var q = ((document.getElementById("ed-guest-q") || {}).value || "").trim().toLowerCase();
      var reg = ((document.getElementById("ed-guest-reg") || {}).value || "");
      var check = ((document.getElementById("ed-guest-check") || {}).value || "");
      var visible = 0;
      document.querySelectorAll("#ed-guest-body tr[data-guest]").forEach(function (row) {
        var belongs = row.getAttribute("data-ed-for") === id && row.getAttribute("data-deleted") !== "1";
        var text = row.textContent.toLowerCase();
        var ok = belongs &&
          (!q || text.indexOf(q) !== -1) &&
          (!reg || row.getAttribute("data-reg") === reg) &&
          (!check || row.getAttribute("data-check") === check);
        row.hidden = !ok;
        if (ok) visible += 1;
      });
      var empty = document.getElementById("ed-guest-empty");
      var hasGuests = eventGuestRows(id).length > 0;
      if (empty) {
        empty.hidden = hasGuests;
        empty.querySelector("td").textContent = hasGuests ? "Không tìm thấy người tham dự." : "Chưa có người tham dự.";
        if (hasGuests && visible === 0) empty.hidden = false;
      }
      var all = document.getElementById("ed-guest-all");
      if (all) all.checked = false;
    }

    function fillEventDetail(row) {
      var id = row.getAttribute("data-event-id") || "";
      var name = row.getAttribute("data-name") || "";
      var code = row.getAttribute("data-code") || "";
      var group = row.getAttribute("data-group") || "";
      var start = row.getAttribute("data-start") || "";
      var end = row.getAttribute("data-end") || "";
      var status = row.getAttribute("data-status") || "preparing";
      document.getElementById("ed-id").value = id;
      document.getElementById("ed-title").textContent = name;
      document.getElementById("ed-meta").textContent = (code ? "#" + code : "") + " · " + group + " · " + formatEventWhen(start) + " – " + formatEventWhen(end);
      var badge = document.getElementById("ed-badge");
      if (badge) {
        badge.className = "badge " + (STATUS_CLASS[status] || "badge-info");
        badge.textContent = STATUS_LABEL[status] || status;
      }
      document.getElementById("ed-group").value = group;
      document.getElementById("ed-name").value = name;
      document.getElementById("ed-start").value = start;
      document.getElementById("ed-end").value = end;
      document.getElementById("ed-place").value = row.getAttribute("data-place") || "";
      document.getElementById("ed-status").value = status;
      document.getElementById("ed-content").value = row.getAttribute("data-content") || "";
      document.getElementById("ed-note").value = row.getAttribute("data-note") || "";
      var gq = document.getElementById("ed-guest-q");
      var greg = document.getElementById("ed-guest-reg");
      var gcheck = document.getElementById("ed-guest-check");
      if (gq) gq.value = "";
      if (greg) greg.value = "";
      if (gcheck) gcheck.value = "";
      renderEventQr(code || id);
      var checkinOpen = document.getElementById("ed-checkin-open");
      if (checkinOpen) checkinOpen.setAttribute("href", "check-in.html#" + id);
      refreshDetailTables();
    }

    function openGuestModal() {
      if (!guestModal) return;
      var q = document.getElementById("guest-user-q");
      if (q) q.value = "";
      renderGuestPicker();
      guestModal.hidden = false;
      guestModal.classList.add("is-open");
      document.body.classList.add("modal-open");
      if (q) q.focus();
    }

    function closeGuestModal() {
      if (!guestModal) return;
      guestModal.classList.remove("is-open");
      guestModal.hidden = true;
      if (!eventModal || eventModal.hidden) document.body.classList.remove("modal-open");
    }

    function addedGuestUserIds() {
      return eventGuestRows(currentEventId()).map(function (row) {
        return row.getAttribute("data-user") || "";
      }).filter(Boolean);
    }

    function syncGuestPickAll() {
      var all = document.getElementById("guest-user-all");
      if (!all) return;
      var boxes = document.querySelectorAll("#guest-user-list input:not(:disabled)");
      var checked = document.querySelectorAll("#guest-user-list input:checked:not(:disabled)");
      all.disabled = !boxes.length;
      all.checked = boxes.length > 0 && checked.length === boxes.length;
      all.indeterminate = checked.length > 0 && checked.length < boxes.length;
    }

    function renderGuestPicker() {
      var list = document.getElementById("guest-user-list");
      var empty = document.getElementById("guest-user-empty");
      if (!list) return;
      var q = ((document.getElementById("guest-user-q") || {}).value || "").trim().toLowerCase();
      var added = addedGuestUserIds();
      var shown = 0;
      list.innerHTML = "";
      EVENT_USERS.forEach(function (user) {
        var hay = (user.name + " " + user.cccd + " " + user.phone + " " + user.email).toLowerCase();
        if (q && hay.indexOf(q) === -1) return;
        shown += 1;
        var already = added.indexOf(user.id) !== -1;
        var row = document.createElement("label");
        row.className = "ed-pick-row" + (already ? " is-added" : "");
        row.setAttribute("role", "option");
        row.innerHTML =
          '<input type="checkbox" value="' + escapeEventHtml(user.id) + '"' + (already ? " checked disabled" : "") + ' aria-label="Chọn ' + escapeEventHtml(user.name) + '">' +
          '<span class="ed-pick-avatar" aria-hidden="true">' + escapeEventHtml(user.initials) + "</span>" +
          '<span class="ed-pick-meta"><strong></strong><span></span></span>' +
          (already ? '<span class="ed-pick-tag">Đã thêm</span>' : "");
        row.querySelector("strong").textContent = user.name;
        row.querySelector(".ed-pick-meta span").textContent = user.email + " · " + user.phone;
        list.appendChild(row);
      });
      if (empty) empty.hidden = shown > 0;
      syncGuestPickAll();
    }

    function appendGuestRow(user) {
      var id = currentEventId();
      if (!id || !user) return;
      var tr = document.createElement("tr");
      tr.setAttribute("data-guest", "");
      tr.setAttribute("data-ed-for", id);
      tr.setAttribute("data-user", user.id);
      tr.setAttribute("data-reg", "pending");
      tr.setAttribute("data-check", "out");
      tr.innerHTML =
        "<td><input type=\"checkbox\" aria-label=\"Chọn " + escapeEventHtml(user.name) + "\"></td>" +
        "<td>" + escapeEventHtml(user.name) + "</td>" +
        "<td>" + escapeEventHtml(user.cccd) + "</td>" +
        "<td>" + escapeEventHtml(user.phone) + "</td>" +
        "<td>" + escapeEventHtml(user.email) + "</td>" +
        "<td><span class=\"badge " + REG_CLASS.pending + "\">" + REG_LABEL.pending + "</span></td>" +
        "<td><span class=\"badge " + CHECK_CLASS.out + "\">" + CHECK_LABEL.out + "</span></td>" +
        "<td><button class=\"doc-action is-danger\" type=\"button\" data-ed-guest-delete aria-label=\"Xóa " + escapeEventHtml(user.name) + "\">" +
        "<svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" aria-hidden=\"true\"><path d=\"M4 7h16\"/><path d=\"M9 7V5h6v2\"/><path d=\"M6 7l1 13h10l1-13\"/></svg></button></td>";
      document.getElementById("ed-guest-body").insertBefore(tr, document.getElementById("ed-guest-empty"));
    }

    function applyEventFilter() {
      var q = ((document.getElementById("event-q") || {}).value || "").trim().toLowerCase();
      var group = ((document.getElementById("event-group") || {}).value || "");
      var status = ((document.getElementById("event-status") || {}).value || "");
      document.querySelectorAll("#event-body tr").forEach(function (row) {
        var text = ((row.getAttribute("data-name") || "") + " " + (row.getAttribute("data-code") || "")).toLowerCase();
        var ok = (!q || text.indexOf(q) !== -1) &&
          (!group || row.getAttribute("data-group") === group) &&
          (!status || row.getAttribute("data-status") === status);
        row.hidden = !ok;
      });
    }

    showEventView();
    window.addEventListener("hashchange", showEventView);

    var dashForm = document.getElementById("event-dash-filter");
    if (dashForm) {
      dashForm.addEventListener("submit", function (event) {
        event.preventDefault();
        showToast(t("eventRefreshed"));
      });
    }

    var listForm = document.getElementById("event-list-filter");
    if (listForm) {
      listForm.addEventListener("submit", function (event) {
        event.preventDefault();
        applyEventFilter();
        showToast(t("eventFiltered"));
      });
    }

    var resetBtn = document.getElementById("event-list-reset");
    if (resetBtn) {
      resetBtn.addEventListener("click", function () {
        var q = document.getElementById("event-q");
        var group = document.getElementById("event-group");
        var status = document.getElementById("event-status");
        if (q) q.value = "";
        if (group) group.value = "";
        if (status) status.value = "";
        applyEventFilter();
        showToast(t("eventReset"));
      });
    }

    document.querySelectorAll("[data-event-create]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        if (eventList) {
          eventDash && (eventDash.hidden = true);
          eventDetail && (eventDetail.hidden = true);
          eventList.hidden = false;
          if (location.hash !== "#danh-sach" && location.hash !== "#tao") {
            history.replaceState(null, "", "su-kien.html#danh-sach");
          }
        }
        openEventModal(null);
      });
    });

    document.querySelectorAll("[data-event-edit]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var row = btn.closest("tr");
        if (row) openEventModal(row);
      });
    });

    document.querySelectorAll("[data-event-delete]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var row = btn.closest("tr");
        if (row) row.hidden = true;
        showToast(t("eventDeleted"));
      });
    });

    if (eventModal) {
      eventModal.addEventListener("click", function (event) {
        if (event.target === eventModal) closeEventModal();
      });
      eventModal.querySelectorAll("[data-event-close]").forEach(function (btn) {
        btn.addEventListener("click", closeEventModal);
      });
    }

    var eventForm = document.getElementById("event-form");
    if (eventForm) {
      eventForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var id = document.getElementById("event-id").value;
        var name = document.getElementById("event-name").value.trim();
        var group = document.getElementById("event-group-input").value.trim();
        var start = document.getElementById("event-start").value;
        var end = document.getElementById("event-end").value;
        var place = document.getElementById("event-place").value.trim();
        var status = document.getElementById("event-status-input").value;
        var content = document.getElementById("event-content").value;
        var note = document.getElementById("event-note").value;
        var row = id ? eventRowById(id) : null;
        if (row) {
          writeEventRow(row, { name: name, group: group, start: start, end: end, place: place, status: status, content: content, note: note });
          showToast(t("eventSaved"));
        } else {
          showToast(t("eventCreated"));
        }
        closeEventModal();
      });
    }

    var edForm = document.getElementById("ed-form");
    if (edForm) {
      edForm.addEventListener("submit", function (event) {
        event.preventDefault();
        var id = currentEventId();
        var row = eventRowById(id);
        if (!row) return;
        var name = document.getElementById("ed-name").value.trim();
        var group = document.getElementById("ed-group").value.trim();
        var start = document.getElementById("ed-start").value;
        var end = document.getElementById("ed-end").value;
        var place = document.getElementById("ed-place").value.trim();
        var status = document.getElementById("ed-status").value;
        var content = document.getElementById("ed-content").value;
        var note = document.getElementById("ed-note").value;
        writeEventRow(row, { name: name, group: group, start: start, end: end, place: place, status: status, content: content, note: note });
        fillEventDetail(row);
        showToast(t("eventSaved"));
      });
    }

    var guestFilter = document.getElementById("ed-guest-filter");
    if (guestFilter) {
      guestFilter.addEventListener("submit", function (event) {
        event.preventDefault();
        applyGuestFilter();
      });
    }

    var guestReset = document.getElementById("ed-guest-reset");
    if (guestReset) {
      guestReset.addEventListener("click", function () {
        var q = document.getElementById("ed-guest-q");
        var reg = document.getElementById("ed-guest-reg");
        var check = document.getElementById("ed-guest-check");
        if (q) q.value = "";
        if (reg) reg.value = "";
        if (check) check.value = "";
        applyGuestFilter();
      });
    }

    var guestAll = document.getElementById("ed-guest-all");
    if (guestAll) {
      guestAll.addEventListener("change", function () {
        document.querySelectorAll("#ed-guest-body tr[data-guest]:not([hidden]) input[type='checkbox']").forEach(function (box) {
          box.checked = guestAll.checked;
        });
      });
    }

    var guestAdd = document.getElementById("ed-guest-add");
    if (guestAdd) guestAdd.addEventListener("click", openGuestModal);

    if (guestModal) {
      guestModal.addEventListener("click", function (event) {
        if (event.target === guestModal) closeGuestModal();
      });
      guestModal.querySelectorAll("[data-guest-close]").forEach(function (btn) {
        btn.addEventListener("click", closeGuestModal);
      });
    }

    var guestUserQ = document.getElementById("guest-user-q");
    if (guestUserQ) {
      guestUserQ.addEventListener("input", renderGuestPicker);
    }

    var guestUserAll = document.getElementById("guest-user-all");
    if (guestUserAll) {
      guestUserAll.addEventListener("change", function () {
        document.querySelectorAll("#guest-user-list input:not(:disabled)").forEach(function (box) {
          box.checked = guestUserAll.checked;
        });
        guestUserAll.indeterminate = false;
      });
    }

    var guestUserList = document.getElementById("guest-user-list");
    if (guestUserList) {
      guestUserList.addEventListener("change", function (event) {
        if (event.target && event.target.matches("input[type='checkbox']")) syncGuestPickAll();
      });
    }

    var guestSave = document.getElementById("guest-save");
    if (guestSave) {
      guestSave.addEventListener("click", function () {
        var boxes = document.querySelectorAll("#guest-user-list input:checked:not(:disabled)");
        if (!boxes.length) {
          showToast(t("eventGuestNeedPick"));
          return;
        }
        var added = 0;
        boxes.forEach(function (box) {
          var user = null;
          EVENT_USERS.forEach(function (item) {
            if (item.id === box.value) user = item;
          });
          if (user) {
            appendGuestRow(user);
            added += 1;
          }
        });
        closeGuestModal();
        refreshDetailTables();
        if (added) showToast(t("eventGuestAdded"));
      });
    }

    var guestBody = document.getElementById("ed-guest-body");
    if (guestBody) {
      guestBody.addEventListener("click", function (event) {
        var btn = event.target.closest("[data-ed-guest-delete]");
        if (!btn) return;
        var row = btn.closest("tr");
        if (!row) return;
        row.setAttribute("data-deleted", "1");
        row.hidden = true;
        refreshDetailTables();
        showToast(t("eventGuestDeleted"));
      });
    }

    var guestDelete = document.getElementById("ed-guest-delete");
    if (guestDelete) {
      guestDelete.addEventListener("click", function () {
        var boxes = document.querySelectorAll("#ed-guest-body tr[data-guest]:not([hidden]) input[type='checkbox']:checked");
        if (!boxes.length) {
          showToast(t("eventGuestNeedSelect"));
          return;
        }
        boxes.forEach(function (box) {
          var row = box.closest("tr");
          if (!row) return;
          row.setAttribute("data-deleted", "1");
          row.hidden = true;
        });
        refreshDetailTables();
        showToast(t("eventGuestDeleted"));
      });
    }

    var attachBtn = document.getElementById("ed-attach");
    var fileInput = document.getElementById("ed-file");
    if (attachBtn && fileInput) {
      attachBtn.addEventListener("click", function () {
        fileInput.click();
      });
      fileInput.addEventListener("change", function () {
        var file = fileInput.files && fileInput.files[0];
        var id = currentEventId();
        if (!file || !id) return;
        var kb = (file.size / 1024).toFixed(1) + " KB";
        var tr = document.createElement("tr");
        tr.setAttribute("data-file", "");
        tr.setAttribute("data-ed-for", id);
        tr.innerHTML =
          "<td><span class=\"ed-file-name\"><svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" aria-hidden=\"true\"><path d=\"M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z\"/><path d=\"M14 3v5h5\"/></svg>" +
          escapeEventHtml(file.name) + "</span></td>" +
          "<td>" + kb + "</td>" +
          "<td><div class=\"event-actions\">" +
          "<button class=\"btn btn-outline btn-sm\" type=\"button\" data-ed-file-download>" +
          "<svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" aria-hidden=\"true\"><path d=\"M12 4v12M7 11l5 5 5-5\"/><path d=\"M5 20h14\"/></svg>Tải xuống</button>" +
          "<button class=\"doc-action is-danger\" type=\"button\" data-ed-file-delete aria-label=\"Xóa tài liệu\">" +
          "<svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" aria-hidden=\"true\"><path d=\"M4 7h16\"/><path d=\"M9 7V5h6v2\"/><path d=\"M6 7l1 13h10l1-13\"/></svg></button></div></td>";
        document.getElementById("ed-file-body").insertBefore(tr, document.getElementById("ed-file-empty"));
        fileInput.value = "";
        refreshDetailTables();
        showToast(t("eventFileAttached"));
      });
    }

    var fileBody = document.getElementById("ed-file-body");
    if (fileBody) {
      fileBody.addEventListener("click", function (event) {
        if (event.target.closest("[data-ed-file-download]")) {
          showToast("Đã tải tài liệu xuống.");
          return;
        }
        var btn = event.target.closest("[data-ed-file-delete]");
        if (!btn) return;
        var row = btn.closest("tr");
        if (!row) return;
        row.setAttribute("data-deleted", "1");
        row.hidden = true;
        refreshDetailTables();
        showToast(t("eventFileDeleted"));
      });
    }
  }

  var ciLoginPanel = document.getElementById("ci-panel-login");
  var ciEventPanel = document.getElementById("ci-panel-event");
  if (ciLoginPanel || ciEventPanel) {
    var CI_NOW = new Date(2026, 8, 11, 10, 0, 0);
    var CHECKIN_EVENTS = {
      e1: {
        name: "test HOP",
        start: "2026-09-10T17:27",
        end: "2026-09-10T18:27",
        place: "Phong 201",
        files: []
      },
      e2: {
        name: "Event test",
        start: "2026-09-10T11:48",
        end: "2026-09-12T12:48",
        place: "Phòng họp",
        files: [{ name: "Hồ sơ năng lực và dự án thực hiện.pdf", href: "assets/demo-nghi-phep.pdf" }]
      }
    };

    function formatCheckinWhen(value) {
      if (!value) return "";
      var parts = value.split("T");
      if (parts.length < 2) return value;
      var d = parts[0].split("-");
      return d[2] + "/" + d[1] + "/" + d[0] + " " + parts[1];
    }

    function parseCheckinDate(value) {
      if (!value) return null;
      var parts = value.split("T");
      if (parts.length < 2) return null;
      var d = parts[0].split("-");
      var t = parts[1].split(":");
      return new Date(+d[0], +d[1] - 1, +d[2], +t[0] || 0, +t[1] || 0, 0);
    }

    function eventPhase(event) {
      var start = parseCheckinDate(event.start);
      var end = parseCheckinDate(event.end);
      if (!start || !end) return "unknown";
      if (CI_NOW < start) return "upcoming";
      if (CI_NOW > end) return "ended";
      return "live";
    }

    function phaseBadge(phase) {
      if (phase === "live") return { cls: "badge badge-live", label: "Đang diễn ra" };
      if (phase === "upcoming") return { cls: "badge badge-info", label: "Chưa bắt đầu" };
      if (phase === "ended") return { cls: "badge badge-muted", label: "Đã kết thúc" };
      return { cls: "badge badge-muted", label: "—" };
    }

    function getCiUser() {
      try { return sessionStorage.getItem("hcs-user") || ""; } catch (e) { return ""; }
    }

    function setCiUser(name) {
      try { sessionStorage.setItem("hcs-user", name); } catch (e) {}
    }

    function rsvpKey(id) { return "hcs-rsvp-" + id; }
    function checkinKey(id) { return "hcs-checkin-" + id; }

    function isRsvpDone(id) {
      try { return sessionStorage.getItem(rsvpKey(id)) === "1"; } catch (e) { return false; }
    }

    function markRsvpDone(id) {
      try { sessionStorage.setItem(rsvpKey(id), "1"); } catch (e) {}
    }

    function isCheckinDone(id) {
      try { return sessionStorage.getItem(checkinKey(id)) === "1"; } catch (e) { return false; }
    }

    function markCheckinDone(id) {
      try { sessionStorage.setItem(checkinKey(id), "1"); } catch (e) {}
    }

    function setCiSteps(active) {
      var order = { login: 0, rsvp: 1, checkin: 2, done: 3 };
      var cur = order[active] || 0;
      document.querySelectorAll("[data-ci-step]").forEach(function (item) {
        var key = item.getAttribute("data-ci-step");
        var idx = order[key] || 0;
        item.classList.remove("is-current", "is-done");
        if (active === "done") {
          item.classList.add("is-done");
          if (key === "checkin") item.classList.add("is-current");
          return;
        }
        if (idx < cur) item.classList.add("is-done");
        if (idx === cur) item.classList.add("is-current");
      });
    }

    function setBanner(kind, message) {
      var banner = document.getElementById("ci-banner");
      var text = document.getElementById("ci-banner-text");
      if (!banner || !text) return;
      banner.hidden = false;
      banner.classList.remove("is-done", "is-wait", "is-warn");
      if (kind) banner.classList.add(kind);
      text.textContent = message;
    }

    function setAction(opts) {
      var btn = document.getElementById("ci-action");
      var label = document.querySelector("#ci-action .btn-label");
      if (!btn) return;
      btn.hidden = !opts.show;
      btn.disabled = !!opts.disabled;
      btn.setAttribute("data-ci-mode", opts.mode || "");
      if (label) label.textContent = opts.label || "";
    }

    function setCheckinPanels(mode) {
      var empty = document.getElementById("ci-empty");
      if (ciLoginPanel) ciLoginPanel.hidden = mode !== "login";
      if (ciEventPanel) ciEventPanel.hidden = mode !== "event";
      if (empty) empty.hidden = mode !== "empty";
      document.body.classList.toggle("is-ci-login", mode === "login");
      document.body.classList.toggle("is-ci-event", mode === "event");
    }

    function fillLoginEventContext(event) {
      var box = document.getElementById("ci-login-event");
      var nameEl = document.getElementById("ci-login-event-name");
      var metaEl = document.getElementById("ci-login-event-meta");
      if (!box || !nameEl || !metaEl) return;
      if (!event) {
        box.hidden = true;
        return;
      }
      box.hidden = false;
      nameEl.textContent = event.name;
      metaEl.textContent = formatCheckinWhen(event.start) + " · " + event.place;
    }

    function renderCheckinFiles(files) {
      var list = document.getElementById("ci-file-list");
      var wrap = document.getElementById("ci-files");
      if (!list || !wrap) return;
      list.innerHTML = "";
      if (!files || !files.length) {
        wrap.hidden = true;
        return;
      }
      wrap.hidden = false;
      files.forEach(function (file) {
        var li = document.createElement("li");
        li.className = "ci-file";
        li.innerHTML =
          '<span class="ci-file-icon" aria-hidden="true"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5"/></svg></span>' +
          '<span class="ci-file-name"></span>' +
          '<a class="ci-file-dl" href="' + file.href + '" download>' +
          'Tải xuống <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path d="M12 4v12M7 11l5 5 5-5"/><path d="M5 20h14"/></svg></a>';
        li.querySelector(".ci-file-name").textContent = file.name;
        list.appendChild(li);
      });
    }

    function showCheckin(id) {
      if (!id) id = "e2";
      var event = CHECKIN_EVENTS[id];
      var userChip = document.getElementById("ci-user-chip");
      var kicker = document.getElementById("ci-kicker");
      var badge = document.getElementById("ci-status-badge");
      var action = document.getElementById("ci-action");
      var skip = document.querySelector(".page-checkin .skip-link");

      if (!event) {
        setCheckinPanels("empty");
        document.title = "Check-in sự kiện — HCS Hành chính số";
        if (skip) skip.setAttribute("href", "#ci-empty");
        return;
      }

      document.title = "Check-in · " + event.name + " — HCS Hành chính số";
      if (action) action.setAttribute("data-event-id", id);

      var user = getCiUser();
      if (!user) {
        setCheckinPanels("login");
        fillLoginEventContext(event);
        if (skip) skip.setAttribute("href", "#ci-username");
        var userInput = document.getElementById("ci-username");
        if (userInput && !userInput.value) userInput.focus();
        return;
      }

      setCheckinPanels("event");
      if (skip) skip.setAttribute("href", "#ci-action");

      document.getElementById("ci-title").textContent = event.name;
      document.getElementById("ci-when").textContent = formatCheckinWhen(event.start) + " – " + formatCheckinWhen(event.end);
      document.getElementById("ci-place").textContent = event.place;

      var phase = eventPhase(event);
      var pb = phaseBadge(phase);
      if (badge) {
        badge.className = pb.cls;
        badge.textContent = pb.label;
      }

      if (userChip) {
        userChip.hidden = false;
        userChip.textContent = "Xin chào, " + user;
      }

      renderCheckinFiles(event.files);

      if (isCheckinDone(id)) {
        setCiSteps("done");
        if (kicker) kicker.textContent = t("eventCheckinKickerCheck");
        setBanner("is-done", t("eventCheckinDone"));
        setAction({ show: true, disabled: true, mode: "done", label: t("eventCheckinBtnDone") });
        return;
      }

      if (!isRsvpDone(id)) {
        setCiSteps("rsvp");
        if (kicker) kicker.textContent = t("eventCheckinKickerRsvp");
        setBanner("", t("eventCheckinHint"));
        setAction({ show: true, disabled: false, mode: "rsvp", label: t("eventCheckinBtn") });
        return;
      }

      setCiSteps("checkin");
      if (kicker) kicker.textContent = t("eventCheckinKickerCheck");

      if (phase === "upcoming") {
        setBanner("is-wait", t("eventRsvpWait"));
        setAction({ show: true, disabled: true, mode: "wait", label: t("eventCheckinBtnGo") });
        return;
      }

      if (phase === "ended") {
        setBanner("is-warn", t("eventCheckinEnded"));
        setAction({ show: true, disabled: true, mode: "ended", label: t("eventCheckinBtnGo") });
        return;
      }

      setBanner("", t("eventCheckinReady"));
      setAction({ show: true, disabled: false, mode: "checkin", label: t("eventCheckinBtnGo") });
    }

    function doCiLogin(username) {
      var name = (username || "").trim();
      if (!name) {
        showToast(t("eventCheckinNeedUser"));
        return;
      }
      setCiUser(name);
      showToast(t("eventCheckinLoginOk"));
      showCheckin((location.hash || "").replace(/^#/, "") || "e2");
    }

    var hashId = (location.hash || "").replace(/^#/, "") || "e2";
    showCheckin(hashId);
    window.addEventListener("hashchange", function () {
      showCheckin((location.hash || "").replace(/^#/, "") || "e2");
    });

    var loginForm = document.getElementById("ci-login");
    if (loginForm) {
      loginForm.addEventListener("submit", function (event) {
        event.preventDefault();
        doCiLogin(document.getElementById("ci-username").value);
      });
    }

    var quickLogin = document.getElementById("ci-quick-login");
    if (quickLogin) {
      quickLogin.addEventListener("click", function () {
        doCiLogin("hungtv");
      });
    }

    var actionBtn = document.getElementById("ci-action");
    if (actionBtn) {
      actionBtn.addEventListener("click", function () {
        if (actionBtn.disabled) return;
        var id = actionBtn.getAttribute("data-event-id") || "e2";
        var mode = actionBtn.getAttribute("data-ci-mode");
        if (mode === "rsvp") {
          markRsvpDone(id);
          showToast(t("eventRsvpOk"));
          showCheckin(id);
          return;
        }
        if (mode === "checkin") {
          var event = CHECKIN_EVENTS[id];
          if (!event || eventPhase(event) !== "live") {
            showToast(t("eventCheckinEnded"));
            showCheckin(id);
            return;
          }
          markCheckinDone(id);
          showToast(t("eventCheckinOk"));
          showCheckin(id);
        }
      });
    }
  }

  var calPage = document.querySelector(".page-cal");
  if (calPage) {
    var TODAY = new Date(2026, 8, 11);
    var cursor = new Date(TODAY.getFullYear(), TODAY.getMonth(), TODAY.getDate());
    var calView = "month";
    var calTypes = { project: true, task: true, event: true };
    var TYPE_LABEL = { project: "Dự án", task: "Công việc", event: "Sự kiện" };
    var DOW = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "CN"];
    var DOW_LONG = ["Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy"];
    var CAL_ITEMS = [
      { id: "p1", type: "project", title: "Xây dựng hệ thống HCS", start: "2026-09-01", end: "2026-09-23" },
      { id: "t1", type: "task", title: "00 giờ avalavi", start: "2026-09-01", end: "2026-09-20" },
      { id: "e0", type: "event", title: "00 giờ Nghỉ lễ 2/9", start: "2026-09-02", end: "2026-09-02" },
      { id: "e1", type: "event", title: "test HOP", start: "2026-09-10", end: "2026-09-10", time: "17:27" },
      { id: "e2", type: "event", title: "Event test", start: "2026-09-10", end: "2026-09-12", time: "11:48" }
    ];

    function pad(n) { return n < 10 ? "0" + n : String(n); }
    function ymd(d) {
      return d.getFullYear() + "-" + pad(d.getMonth() + 1) + "-" + pad(d.getDate());
    }
    function parseDay(str) {
      var p = String(str).split("-");
      return new Date(+p[0], +p[1] - 1, +p[2]);
    }
    function sameDay(a, b) {
      return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
    }
    function addDays(d, n) {
      return new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);
    }
    function startOfWeek(d) {
      var day = d.getDay();
      return addDays(d, day === 0 ? -6 : 1 - day);
    }
    function endOfWeek(d) { return addDays(startOfWeek(d), 6); }
    function startOfMonth(d) { return new Date(d.getFullYear(), d.getMonth(), 1); }
    function endOfMonth(d) { return new Date(d.getFullYear(), d.getMonth() + 1, 0); }
    function fmtVN(d) {
      return pad(d.getDate()) + "/" + pad(d.getMonth() + 1) + "/" + d.getFullYear();
    }
    function titleForView() {
      if (calView === "day") {
        return DOW_LONG[cursor.getDay()] + ", " + fmtVN(cursor);
      }
      if (calView === "week") {
        return fmtVN(startOfWeek(cursor)) + " — " + fmtVN(endOfWeek(cursor));
      }
      return "Tháng " + (cursor.getMonth() + 1) + " " + cursor.getFullYear();
    }
    function visibleRange() {
      if (calView === "day") return { start: cursor, end: cursor };
      if (calView === "week") return { start: startOfWeek(cursor), end: endOfWeek(cursor) };
      return { start: startOfMonth(cursor), end: endOfMonth(cursor) };
    }
    function overlaps(item, start, end) {
      var a = parseDay(item.start);
      var b = parseDay(item.end);
      return a <= end && b >= start;
    }
    function visibleItems(start, end) {
      var q = ((document.getElementById("cal-q") || {}).value || "").trim().toLowerCase();
      return CAL_ITEMS.filter(function (item) {
        return calTypes[item.type] &&
          (!q || item.title.toLowerCase().indexOf(q) !== -1) &&
          (!start || overlaps(item, start, end));
      }).sort(function (a, b) {
        return a.start < b.start ? -1 : a.start > b.start ? 1 : 0;
      });
    }
    function escCal(value) {
      return String(value == null ? "" : value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
    }
    function dowRow() {
      return '<div class="cal-dows">' + DOW.map(function (d) {
        return '<div class="cal-dow">' + d + "</div>";
      }).join("") + "</div>";
    }
    function barsForWeek(weekStart, items) {
      var weekEnd = addDays(weekStart, 6);
      return items.filter(function (item) {
        return overlaps(item, weekStart, weekEnd);
      }).map(function (item) {
        var a = parseDay(item.start);
        var b = parseDay(item.end);
        var from = a < weekStart ? weekStart : a;
        var to = b > weekEnd ? weekEnd : b;
        var col = Math.round((from - weekStart) / 86400000) + 1;
        var span = Math.round((to - from) / 86400000) + 1;
        var label = (item.time ? item.time + " · " : "") + item.title;
        return '<button class="cal-bar is-' + item.type + '" type="button" style="grid-column:' + col + " / span " + span + '" data-cal-id="' + item.id + '">' + escCal(label) + "</button>";
      }).join("");
    }
    function weekBlock(weekStart, inMonth, tall) {
      var nums = "";
      for (var i = 0; i < 7; i += 1) {
        var d = addDays(weekStart, i);
        var cls = "cal-cell";
        if (inMonth != null && d.getMonth() !== inMonth) cls += " is-muted";
        if (sameDay(d, TODAY)) cls += " is-today";
        nums += '<button class="' + cls + '" type="button" data-cal-date="' + ymd(d) + '"><span class="cal-num">' + d.getDate() + "</span></button>";
      }
      var items = visibleItems(weekStart, addDays(weekStart, 6));
      return '<div class="cal-week-block' + (tall ? " is-tall" : "") + '"><div class="cal-daynums">' + nums + '</div><div class="cal-lanes">' + barsForWeek(weekStart, items) + "</div></div>";
    }
    function renderMonth() {
      var monthStart = startOfMonth(cursor);
      var gridStart = startOfWeek(monthStart);
      var monthEnd = endOfMonth(cursor);
      var html = '<div class="cal-sheet">' + dowRow();
      var week = new Date(gridStart.getTime());
      while (week <= monthEnd) {
        html += weekBlock(week, cursor.getMonth(), false);
        week = addDays(week, 7);
      }
      document.getElementById("cal-view-month").innerHTML = html + "</div>";
    }
    function renderWeek() {
      var ws = startOfWeek(cursor);
      document.getElementById("cal-view-week").innerHTML =
        '<div class="cal-sheet">' + dowRow() + weekBlock(ws, null, true) + "</div>";
    }
    function renderDay() {
      var items = visibleItems(cursor, cursor);
      var body = items.length ? items.map(function (item) {
        return '<article class="cal-agenda-item"><div><strong>' + escCal(item.title) + "</strong><p>" +
          (item.time ? item.time + " · " : "Cả ngày · ") + TYPE_LABEL[item.type] + " · " +
          fmtVN(parseDay(item.start)) + (item.start !== item.end ? " — " + fmtVN(parseDay(item.end)) : "") +
          "</p></div><span class=\"badge " + (item.type === "event" ? "badge-live" : "badge-info") + '">' +
          TYPE_LABEL[item.type] + "</span></article>";
      }).join("") : '<p class="empty">Không có lịch trong ngày này.</p>';
      document.getElementById("cal-view-day").innerHTML =
        '<div class="cal-agenda"><h3>' + DOW_LONG[cursor.getDay()] + ", " + fmtVN(cursor) + "</h3>" + body + "</div>";
    }
    function renderList() {
      var range = visibleRange();
      var items = visibleItems(range.start, range.end);
      var body = items.length ? items.map(function (item) {
        return '<article class="cal-list-item"><div><strong>' + escCal(item.title) + "</strong><p>" +
          TYPE_LABEL[item.type] + " · " + fmtVN(parseDay(item.start)) +
          (item.start !== item.end ? " — " + fmtVN(parseDay(item.end)) : "") +
          (item.time ? " · " + item.time : "") +
          "</p></div><span class=\"badge " + (item.type === "event" ? "badge-live" : "badge-info") + '">' +
          TYPE_LABEL[item.type] + "</span></article>";
      }).join("") : '<p class="empty">Không có mục lịch trong khoảng đang xem.</p>';
      document.getElementById("cal-view-list").innerHTML = '<div class="cal-agenda">' + body + "</div>";
    }
    function showCalView(name, skipRender) {
      calView = name;
      ["month", "week", "day", "list"].forEach(function (view) {
        var pane = document.getElementById("cal-view-" + view);
        if (pane) pane.hidden = view !== name;
      });
      document.querySelectorAll("[data-cal-view]").forEach(function (btn) {
        var on = btn.getAttribute("data-cal-view") === name;
        btn.classList.toggle("is-active", on);
        btn.setAttribute("aria-selected", on ? "true" : "false");
      });
      if (!skipRender) renderCal();
    }
    function renderCal() {
      var title = document.getElementById("cal-title");
      if (title) title.textContent = titleForView();
      if (calView === "week") renderWeek();
      else if (calView === "day") renderDay();
      else if (calView === "list") renderList();
      else renderMonth();
    }
    function shiftCursor(dir) {
      if (calView === "day") cursor = addDays(cursor, dir);
      else if (calView === "week") cursor = addDays(cursor, dir * 7);
      else cursor = new Date(cursor.getFullYear(), cursor.getMonth() + dir, 1);
      renderCal();
    }

    document.querySelectorAll("[data-cal-view]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        showCalView(btn.getAttribute("data-cal-view"));
      });
    });
    document.querySelectorAll("[data-cal-type]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var type = btn.getAttribute("data-cal-type");
        calTypes[type] = !calTypes[type];
        btn.classList.toggle("is-active", calTypes[type]);
        btn.setAttribute("aria-pressed", calTypes[type] ? "true" : "false");
        renderCal();
      });
    });
    var calForm = document.getElementById("cal-search-form");
    if (calForm) {
      calForm.addEventListener("submit", function (event) {
        event.preventDefault();
        renderCal();
        showToast(t("calFiltered"));
      });
    }
    var calReset = document.getElementById("cal-reset");
    if (calReset) {
      calReset.addEventListener("click", function () {
        var q = document.getElementById("cal-q");
        if (q) q.value = "";
        Object.keys(calTypes).forEach(function (key) { calTypes[key] = true; });
        document.querySelectorAll("[data-cal-type]").forEach(function (btn) {
          btn.classList.add("is-active");
          btn.setAttribute("aria-pressed", "true");
        });
        renderCal();
        showToast(t("calReset"));
      });
    }
    var prev = document.getElementById("cal-prev");
    var next = document.getElementById("cal-next");
    var todayBtn = document.getElementById("cal-today");
    if (prev) prev.addEventListener("click", function () { shiftCursor(-1); });
    if (next) next.addEventListener("click", function () { shiftCursor(1); });
    if (todayBtn) {
      todayBtn.addEventListener("click", function () {
        cursor = new Date(TODAY.getFullYear(), TODAY.getMonth(), TODAY.getDate());
        renderCal();
      });
    }
    calPage.addEventListener("click", function (event) {
      var dayBtn = event.target.closest("[data-cal-date]");
      if (dayBtn) {
        cursor = parseDay(dayBtn.getAttribute("data-cal-date"));
        showCalView("day");
        return;
      }
      var bar = event.target.closest(".cal-bar");
      if (bar) showToast(bar.textContent.trim());
    });

    renderCal();
  }
})();
