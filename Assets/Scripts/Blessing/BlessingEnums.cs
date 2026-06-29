/// <summary>
/// BlessingEnums.cs
/// Định nghĩa toàn bộ enum dùng cho hệ thống Chúc Phúc Anh Linh (Blessing) của S03.
/// Các anh hùng lịch sử Việt Nam thay thế pantheon nước ngoài (tham khảo Hades/SWORN).
/// </summary>

// ──────────────────────────────────────────────────────────────────
// Độ hiếm của Blessing
// ──────────────────────────────────────────────────────────────────
public enum BlessingRarity
{
    Common,       // Thường     – trọng số rơi cao nhất
    Rare,         // Hiếm
    Epic,         // Sử Thi
    Legendary     // Huyền Thoại – chỉ rơi 1 lần (maxStack = 1)
}

// ──────────────────────────────────────────────────────────────────
// Nhánh anh hùng – quyết định màu sắc, chủ đề thiết kế
// ──────────────────────────────────────────────────────────────────
public enum HeroType
{
    AnDuongVuong,  // Phòng thủ, No Thần, Cổ Loa Thành
    TrungTrac,     // Ý chí chiến đấu, hồi năng lượng, hồi sinh
    TrungNhi,      // Tốc độ, Dash damage, bóng chiến trường
    QuangTrung     // Tấn công bùng phát, chí mạng, set đánh
}

// ──────────────────────────────────────────────────────────────────
// Loại hiệu ứng Blessing – mỗi loại ánh xạ sang 1 hệ thống gameplay
// ──────────────────────────────────────────────────────────────────
public enum BlessingEffectType
{
    // ── An Dương Vương ──────────────────────────────────────────
    Armor,             // Giáp: giảm sát thương nhận vào
    DivineCrossbow,    // Nỏ Thần: đòn đánh thứ 5 bắn thêm tên năng lượng
    DashBarrier,       // Tường Thành: Dash tạo kết giới làm chậm địch
    Awareness,         // Cảnh Giới: báo sớm wave + tăng tầm phát hiện
    CoLoaCitadel,      // Thành Cổ Loa (Ultimate): định kỳ tạo lá chắn

    // ── Trưng Trắc ──────────────────────────────────────────────
    LowHealthDamage,   // Hiệu Triệu: máu thấp → sát thương cao
    AttackSpeed,       // Cờ Khởi Nghĩa: tăng tốc độ đánh
    KillSkillEnergy,   // Khởi Nghĩa Mê Linh: hạ quái hồi Dash + máu
    Revive,            // Nữ Vương: hồi sinh khi chết (maxStack = 1)
    Uprising,          // Hai Bà Khởi Nghĩa (Ultimate): AT tăng theo số địch xung quanh

    // ── Trưng Nhị ───────────────────────────────────────────────
    MoveSpeed,         // Kỵ Tướng: tăng tốc độ di chuyển
    DashDamage,        // Xung Phong: Dash gây sát thương trên đường lướt
    PostDashDamage,    // Truy Kích: đòn đầu tiên sau Dash gây thêm sát thương
    DashDecoy,         // Bóng Chiến Trường: Dash tạo phân thân làm rối loạn địch
    WarElephant,       // Voi Chiến (Ultimate): Dash xuyên địch gây sát thương lớn

    // ── Quang Trung ─────────────────────────────────────────────
    CriticalPower,     // Đống Đa: tăng sát thương chí mạng
    DashCooldown,      // Thần Tốc Bắc Tiến: giảm hồi Dash
    CriticalLightning, // Thiên Lôi Tây Sơn: chí mạng → xét xét tỉ lệ gọi sét
    KyDauFrenzy        // Xuân Kỷ Dậu (Ultimate): cuồng chiến ngắn
}
