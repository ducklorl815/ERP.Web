// 收合功能
function toggleSection(sectionId) {
    const section = document.getElementById(sectionId);
    const header = section.previousElementSibling;
    const icon = header.querySelector(".toggle-icon");

    if (section.style.display === "none") {
        section.style.display = "block";
        icon.classList.replace("fa-chevron-right", "fa-chevron-down");
    } else {
        section.style.display = "none";
        icon.classList.replace("fa-chevron-down", "fa-chevron-right");
    }
}

// 預設收合狀態 (可自行決定是否要一開始就收起)
document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".card-body").forEach(el => el.style.display = "inline");
    document.querySelectorAll(".toggle-icon").forEach(icon => {
        icon.classList.remove("fa-chevron-down");
        icon.classList.add("fa-chevron-right");
    });
});