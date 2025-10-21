document.addEventListener("DOMContentLoaded", function () {
    initTreeEvents();
});

function initTreeEvents() {
    const tree = document.getElementById("controllerTree");
    if (!tree) return;

    // 展開/收合
    tree.querySelectorAll(".toggle-icon").forEach(icon => {
        icon.addEventListener("click", e => {
            e.stopPropagation();
            const li = icon.closest(".tree-node");
            li.classList.toggle("collapsed");
            li.classList.toggle("expanded");
        });
    });

    // 初始化全部收合
    tree.querySelectorAll(".tree-node").forEach(li => {
        if (li.querySelector("ul")) li.classList.add("collapsed");
    });

    // Checkbox 父子聯動
    tree.querySelectorAll(".tree-checkbox").forEach(chk => {
        chk.addEventListener("change", e => {
            e.stopPropagation();
            const li = chk.closest(".tree-node");

            // 子節點跟隨父勾選
            li.querySelectorAll("ul .tree-checkbox").forEach(c => { c.checked = chk.checked; c.indeterminate = false; });

            // 勾選父節點 → 展開整棵樹
            if (chk.checked) expandTree(li);

            // 更新父節點半勾
            updateParentState(li);
        });
    });

    // 展開父子樹
    function expandTree(li) {
        if (!li) return;
        li.classList.remove("collapsed"); li.classList.add("expanded");
        li.querySelectorAll("ul li.tree-node").forEach(c => { c.classList.remove("collapsed"); c.classList.add("expanded"); });
        const parentLi = li.parentElement.closest(".tree-node");
        if (parentLi) expandTree(parentLi);
    }

    // 遞迴更新父節點半勾
    function updateParentState(li) {
        const parentLi = li.parentElement.closest(".tree-node");
        if (!parentLi) return;

        const parentChk = parentLi.querySelector(".node-content .tree-checkbox");

        // 取得 parentLi 直接子層的 checkbox
        const siblingCheckboxes = parentLi.querySelectorAll(':scope > ul > .tree-node > .node-content > .tree-checkbox');

        const allChecked = Array.from(siblingCheckboxes).every(c => c.checked);
        const anyChecked = Array.from(siblingCheckboxes).some(c => c.checked || c.indeterminate);
        const noneChecked = Array.from(siblingCheckboxes).every(c => !c.checked && !c.indeterminate);

        if (allChecked) {
            // 全勾選
            parentChk.checked = true;
            parentChk.indeterminate = false;
        } else if (noneChecked) {
            // 全沒勾
            parentChk.checked = false;
            parentChk.indeterminate = false;
        } else if (anyChecked) {
            // 有部分勾選或半勾
            parentChk.checked = false;
            parentChk.indeterminate = true;
        }

        // 遞迴往上更新
        updateParentState(parentLi);
    }
    // === 初始化父節點的半勾 / 全勾狀態 ===
    function initParentCheckState() {
        const allNodes = tree.querySelectorAll(".tree-node");
        // 反向排序，先處理底層節點，確保狀態正確往上更新
        Array.from(allNodes).reverse().forEach(li => {
            updateParentState(li);
        });
    }
    // === 自動展開有勾選的節點 ===
    function expandCheckedNodes() {
        const checkedNodes = tree.querySelectorAll(".tree-checkbox:checked");

        checkedNodes.forEach(chk => {
            let li = chk.closest(".tree-node");
            while (li) {
                li.classList.remove("collapsed");
                li.classList.add("expanded");

                // 向上找父層，逐層展開
                li = li.parentElement.closest(".tree-node");
            }
        });
    }


    // === 畫面載入完成後初始化 ===
    initParentCheckState();
    expandCheckedNodes();

    // 送出按鈕
    const submitBtn = document.getElementById("submitGroup");
    submitBtn.replaceWith(submitBtn.cloneNode(true)); // 解除所有舊事件

    const newSubmitBtn = document.getElementById("submitGroup");
    newSubmitBtn.addEventListener("click", () => {
        const container = document.getElementById("moduleContainer");
        const modules = [...container.querySelectorAll('.module-row')]
            .map(row => {
                const select = row.querySelector('select');
                const descEl = row.querySelector('.module-desc');
                return {
                    id: select.value || "",
                    name: select.options[select.selectedIndex]?.text || "",
                    desc: descEl?.textContent.trim() || ""
                };
            })
            .filter(m => m.id !== "");

        const id = modules.length > 0 ? modules[0].id : "";

        const inputName = document.getElementById("groupName");
        const inputDesc = document.getElementById("groupDesc");
        const selectedTree = getCheckedTree(document.querySelector("#controllerTree > ul"));

        const data = {
            ID: id,
            groupName: inputName.value,
            groupDesc: inputDesc.value,
            selectedModules: modules,
            selectedNodes: selectedTree
        };

        console.log("送出資料：", data);

        $.ajax({
            url: '/ControllerSetting/UpdateAccessGroup',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function (res) {
                alert("送出成功");
                window.location.reload();
            },
            error: function (err) {
                console.error(err);
                alert("送出失敗");
            }
        });
    });

    // 遞迴抓勾選節點
    function getCheckedTree(ul) {
        if (!ul) return [];
        const result = [];

        ul.querySelectorAll(":scope > li.tree-node").forEach(li => {
            const chk = li.querySelector(":scope > .node-content > .tree-checkbox");
            if (!chk) return;

            const childUl = li.querySelector(":scope > ul.tree-list");
            const children = getCheckedTree(childUl);

            // 先嘗試常見 class，容錯以前你改過的命名或多了一層包裹
            let textEl = li.querySelector(".node-name")
                || li.querySelector(".node-text")
                || li.querySelector(".node-text-group");

            let text = "";
            if (textEl) {
                // 如果選到的是 group，嘗試取裡面的 .node-name；沒有則取 element 的 innerText
                const innerName = (textEl.querySelector && textEl.querySelector(".node-name"))
                    ? textEl.querySelector(".node-name").innerText
                    : textEl.innerText;
                text = innerName ? innerName.trim() : "";
            }

            if (chk.checked || (children && children.length > 0)) {
                result.push({
                    ID: li.dataset.id,
                    DisplayName: text,
                    children: children.length ? children : null
                });
            }
        });

        return result;
    }
}