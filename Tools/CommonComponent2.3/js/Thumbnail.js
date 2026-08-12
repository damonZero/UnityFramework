
const eventJson = 'eventJson'

var sizeMap = {}

var curMenuVal = ""

var curTabColor = "#c2c2c2"

// 备注相关：正在编辑的组件缩略图路径、备注图片列表、悬浮提示定时器
var curModifyThumb = null
var curRemarkThumb = null
var curRemarkImages = []
var remarkTooltipTimer = null
var remarkHoverCard = null
// 新增面板选择的备注图片（源路径，新增成功时再复制进组件目录）
var curAddRemarkImages = []

function initThunbnails() {
    $(".dragAdd").on('dragover', function (event) {
        event.preventDefault();
    });

    $(".dragAdd").on('drop', function (event) {
        event.preventDefault();
        var jsonStr = event.originalEvent.dataTransfer.getData(eventJson)
        addPsd(jsonStr);
        alert(jsonStr)
    });

    // 备注悬浮提示：用全局 mousemove 判断鼠标是否仍在卡片/弹窗范围内，
    // 只有当鼠标同时离开两者时才隐藏，避免鼠标从卡片移到弹窗的途中
    // （间隔区域触发 mouseleave）导致弹窗提前消失
    $(document).on('mousemove', function (e) {
        if (!remarkHoverCard) {
            return;
        }
        if (!$('#remarkTooltip').is(':visible')) {
            return;
        }
        var tolerance = 10; // 桥接卡片与弹窗之间的间隔
        var overCard = isPointInElement(e.clientX, e.clientY, remarkHoverCard, tolerance);
        var overTip = isPointInElement(e.clientX, e.clientY, document.getElementById('remarkTooltip'), tolerance);
        if (overCard || overTip) {
            if (remarkTooltipTimer) {
                clearTimeout(remarkTooltipTimer);
                remarkTooltipTimer = null;
            }
        } else {
            hideRemarkTooltip();
        }
    });

    // 备注编辑弹窗：添加图片按钮 / 选择文件
    $('#addRemarkImageBtn').on('click', function () {
        $('#remarkImageInput').trigger('click');
    });
    $('#remarkImageInput').on('change', function (event) {
        addRemarkImages(event.target.files);
    });

    // 新增面板：备注图片按钮 / 选择文件
    $('#addPanelImageBtn').on('click', function () {
        $('#addPanelImageInput').trigger('click');
    });
    $('#addPanelImageInput').on('change', function (event) {
        addPanelRemarkImages(event.target.files);
    });

    // updateThumbnail();
}

// 判断点 (mx,my) 是否落在元素矩形内（tolerance 为向外扩的容差像素）
function isPointInElement(mx, my, el, tolerance) {
    if (!el) {
        return false;
    }
    tolerance = tolerance || 0;
    var r = el.getBoundingClientRect();
    return mx >= r.left - tolerance && mx <= r.right + tolerance &&
           my >= r.top - tolerance && my <= r.bottom + tolerance;
}

function showChooseThumbnail(url) {
    $('#chooseThumbnail').empty();
    $('<img src="' + url + '" />').appendTo('#chooseThumbnail');
}

function updateThumbnail() {
    var query = $('#search-box').val() + ' ' + curMenuVal;
    $('#container').empty();
    const jsonArr = getJsonInfo(query);
    jsonArr.sort(function (a, b) {
        return a.layerName.localeCompare(b.layerName);
    });
    for (let i = 0; i < jsonArr.length; i++) {
        var isFirst = false;
        if (i == 0 || (i % 4) == 0) {
            isFirst = true;
        }
        var isEnd = false;
        if (i == jsonArr.length - 1 || (i + 1) % 4 == 0) {
            isEnd = true;
        }

        ShowThumbnail(jsonArr[i], isFirst, isEnd);
    }
    setBgColor(curTabColor);
}

function ShowThumbnail(json, isFirst, isEnd) {
    var thumbPath = json.thumbnailPath
    var eventData = encodeURI(`${json.psdPath}&${json.layerName}`)
    //var thumb = $('<div class="image-wrapper"><img class="thumbnail" src="' + thumbPath + '" draggable="true"></img><label class="imgName"></label></div>').appendTo('#container');
    var thumb = $('<div class="layui-col-xs3"><div class="layui-card"><div class="layui-card-header"><label class="imgName"></label></div><div class="layui-card-body"><div class="imgContainer"><img class="layui-col-xs3 thumbnail " src="' + thumbPath + '" draggable="true"></img></div><div class="buttonContainer"></div></div></div></div>').appendTo('#container');
    thumb.find('.imgContainer').css({
        width: '250px',
        height: '250px',
    });
    thumb.find('.thumbnail').css({
        width: '70%',
        height: '70%',
        objectFit: 'contain',
        marginLeft: '10px'
    });
    thumb.find('.imgName').css({
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center'
    });
    thumb.find('.thumbnail').css({
        width: '70%',
        height: '70%',
        objectFit: 'contain',
        marginLeft: '10px'
    });

    var buttonContainer = thumb.find('.buttonContainer');
    buttonContainer.css({
        display: 'flex',
        justifyContent: 'center',
        marginTop: '-50px'
    });

    var buttonContainer = thumb.find('.buttonContainer');

    // 添加 layui 按钮到 .buttonContainer 内
    var button = $('<button class="layui-btn layui-btn-primary">添加</button>').appendTo(buttonContainer);

    // 添加按钮的点击事件监听
    button.on('click', function () {
        forceHideRemarkTooltip();
        addPsd(eventData);
    });

    var label = thumb.find('.imgName');
    label.text(json.layerName);

    var img = thumb.find('.thumbnail');
    img.on('load', function () {
        var image = $(this);
        var src = image.attr('src');
        sizeMap[src] = [image.width(), image.height()];
    });
    img.click(function () {
        forceHideRemarkTooltip();
        // $('.thumbnail').removeClass('selected');
        // img.addClass('selected');
        // showModifyInfo(img.attr('src'));
        var images = [];
        var url = $(this).attr("src");
        var img = "<img src='" + url + "'/>";
        images.push({
            src: url,
            alt: '图片', // 图片的标题或描述，根据实际情况进行修改
            style: 'max-width: 100%; max-height: 100%;' // 图片的样式，这里设置为 100% 大小显示
        })

        layer.photos({
            photos: {
                title: '图片浏览器', // 图片浏览器的标题，根据实际情况进行修改
                data: images // 图片的数组
            },
            anim: 5 // 图片浏览器的动画效果，可根据实际需要进行修改
        });

    });
    img.on('dragstart', function (event) {
        console.log(`dragstart ${thumbPath}`)
        event.originalEvent.dataTransfer.setData(eventJson, eventData);
        event.originalEvent.dataTransfer.dropEffect = 'copy';
    });

    // 添加 layui 按钮到 .buttonContainer 内
    var modifyButton = $('<button class="layui-btn layui-btn-primary">修改</button>').appendTo(buttonContainer);

    // 添加按钮的点击事件监听
    modifyButton.on('click', function () {
        forceHideRemarkTooltip();
        showModifyInfo(img.attr('src'));
        onModifyBtn();
    });

    // 鼠标 hover 卡片时在侧边弹出备注弹窗
    thumb.on('mouseenter', function (e) {
        showRemarkTooltip(e, json);
    });
    thumb.on('mouseleave', function () {
        hideRemarkTooltip();
    });
}


function setBgColor(color) {
    var itemli = document.getElementById("container").getElementsByClassName("layui-card");
    for (var i = 0; i < itemli.length; i++) {
        itemli[i].style.backgroundColor = color;
    }
}

function UpdateThumbnailScale(pct) {
    $('.thumbnail').each(function () {
        var image = $(this);
        var src = image.attr('src');
        var originalSize = sizeMap[src];
        var originalWidth = originalSize[0];
        var originalHeight = originalSize[1];
        var targetWidth = originalWidth * pct;
        var targetHeight = originalHeight * pct;
        image.css('width', targetWidth + 'px');
        image.css('height', targetHeight + 'px');
    });
}

function getCurMenuVal() {
    return curMenuVal;
}

function setCurMenuVal(newVal) {
    curMenuVal = newVal;
    updateThumbnail();
}

function getCurTabColor(){
    return curTabColor;
}

function setCurTabColor(color){
    curTabColor = color;
    setBgColor(curTabColor);
}

/**
 * 备注悬浮提示：在鼠标 hover 卡片时于侧边弹出。
 * 内容分为两部分：上面是备注文本，下面是备注图片缩略图。
 */
function showRemarkTooltip(event, json) {
    var remark = json.remark || {};
    var text = (remark.text || '').trim();
    var images = Array.isArray(remark.images) ? remark.images : [];

    // 没有备注内容（无文本且无图片）时不弹出弹窗
    if (!text && images.length === 0) {
        forceHideRemarkTooltip();
        return;
    }

    if (remarkTooltipTimer) {
        clearTimeout(remarkTooltipTimer);
        remarkTooltipTimer = null;
    }

    var html = '';
    html += '<div class="remark-tooltip-title">' + escapeHtml(json.layerName) + '</div>';
    html += '<div class="remark-tooltip-text">' + escapeHtml(text) + '</div>';
    if (images.length > 0) {
        html += '<div class="remark-tooltip-images">';
        for (var i = 0; i < images.length; i++) {
            html += '<img class="remark-tooltip-img" src="' + images[i] + '" data-index="' + i + '">';
        }
        html += '</div>';
    }

    var tooltip = $('#remarkTooltip');
    tooltip.html(html).show();

    // 记录当前 hover 的卡片，供全局 mousemove 判断是否该隐藏弹窗
    remarkHoverCard = event.currentTarget;

    // 点击图片放大展示原始图片尺寸
    tooltip.find('.remark-tooltip-img').on('click', function () {
        forceHideRemarkTooltip();
        showPhotoViewer(images);
    });

    // 定位到卡片侧边：优先显示在右侧，超出视口宽度则翻转到左侧。
    // 与卡片边缘轻微重叠，保证鼠标从卡片移到弹窗的路径连续（不会触发 mouseleave）
    var rect = event.currentTarget.getBoundingClientRect();
    var tw = tooltip.outerWidth();
    var th = tooltip.outerHeight();
    var vw = document.documentElement.clientWidth;
    var vh = document.documentElement.clientHeight;

    var left = rect.right - 6;
    if (left + tw > vw) {
        left = rect.left - tw + 6;
    }
    if (left < 0) {
        left = 8;
    }
    var top = rect.top;
    if (top + th > vh) {
        top = vh - th - 8;
    }
    if (top < 0) {
        top = 8;
    }
    tooltip.css({ left: left + 'px', top: top + 'px' });
}

function hideRemarkTooltip() {
    if (remarkTooltipTimer) {
        clearTimeout(remarkTooltipTimer);
    }
    remarkTooltipTimer = setTimeout(function () {
        $('#remarkTooltip').hide();
        remarkHoverCard = null;
    }, 100);
}

// 立即隐藏备注悬浮提示（点击卡片按钮/图片等场景）
function forceHideRemarkTooltip() {
    if (remarkTooltipTimer) {
        clearTimeout(remarkTooltipTimer);
        remarkTooltipTimer = null;
    }
    $('#remarkTooltip').hide();
    remarkHoverCard = null;
}

// 用 layui 图片浏览器放大展示图片（原始尺寸）
function showPhotoViewer(urls) {
    var data = [];
    for (var i = 0; i < urls.length; i++) {
        data.push({
            src: urls[i],
            alt: '备注图片',
            style: 'max-width: 100%; max-height: 100%;'
        });
    }
    layer.photos({
        photos: {
            title: '图片预览',
            data: data
        },
        anim: 5
    });
}

/**
 * 打开“编辑备注”弹窗：可增删改备注的文本和图片。
 * 需先点击组件的“修改”按钮（会设置 curModifyThumb）。
 */
function openRemarkEdit() {
    if (!curModifyThumb) {
        alert('请先在组件卡片上点击“修改”按钮，再编辑备注');
        return;
    }
    var data = imgJsonMap[curModifyThumb];
    if (!data) {
        alert('没有找到组件配置，请重新选择');
        return;
    }
    var json = data.json;

    // 记录本次编辑的目标组件，避免修改面板关闭后丢失目标
    curRemarkThumb = curModifyThumb;
    var remark = json.remark || {};
    curRemarkImages = Array.isArray(remark.images) ? remark.images.slice() : [];

    $('#remarkText').val(remark.text || '');
    renderRemarkImages();

    layer.open({
        type: 1,
        title: '编辑备注 - ' + json.layerName,
        closeBtn: true,
        shift: 2,
        area: ['520px', '480px'],
        shadeClose: false,
        btn: ['保存', '取消'],
        btnAlign: 'c',
        content: $('#remark-main'),
        btn1: function (index) {
            // 保存成功则关闭编辑弹窗（layui 对第一个按钮不校验返回值，需手动关闭）
            if (saveRemarkToJson()) {
                layer.close(index);
            }
            return false;
        },
        btn2: function () {
            return true;
        },
        end: function () {
            curRemarkImages = [];
            curRemarkThumb = null;
        }
    });
}

// 渲染备注编辑弹窗里的图片列表（每个带删除按钮，点击缩略图可预览）
function renderRemarkImages() {
    var box = $('#remarkImages');
    box.empty();
    if (curRemarkImages.length === 0) {
        box.append('<div class="remark-empty">暂无图片，点击“添加图片”选择图片</div>');
        return;
    }
    for (var i = 0; i < curRemarkImages.length; i++) {
        (function (idx) {
            var url = curRemarkImages[idx];
            var item = $('<div class="remark-image-item"></div>');
            $('<img>').attr('src', url).on('click', function () {
                showPhotoViewer(curRemarkImages);
            }).appendTo(item);
            $('<button type="button" class="remark-image-del" title="删除">×</button>').on('click', function () {
                curRemarkImages.splice(idx, 1);
                renderRemarkImages();
            }).appendTo(item);
            box.append(item);
        })(i);
    }
}

// 保存备注到组件 JSON
function saveRemarkToJson() {
    if (!curRemarkThumb) {
        alert('组件信息缺失，请重新选择组件');
        return false;
    }
    var data = imgJsonMap[curRemarkThumb];
    if (!data) {
        alert('未找到组件配置，可能已被修改或删除，请重新选择');
        return false;
    }
    var json = data.json;
    var remarkText = $('#remarkText').val().trim();
    var remarkImages = curRemarkImages.slice();

    var jsonData = {
        'psdPath': json.psdPath,
        'thumbnailPath': json.thumbnailPath,
        'tags': json.tags,
        'layerName': json.layerName,
        'remark': {
            text: remarkText,
            images: remarkImages
        }
    };

    if (writeJsonHandlerSilent(jsonData)) {
        // 写入成功后再更新内存对象（保持绝对路径），失败则不污染内存缓存
        json.remark = {
            text: remarkText,
            images: remarkImages
        };
        resetCash();
        updateThumbnail();
        alert('备注保存成功');
        return true;
    }
    return false;
}

// 把用户选择的图片复制进组件目录，并加入备注图片列表
function addRemarkImages(fileList) {
    if (!fileList || fileList.length === 0) {
        return;
    }
    var data = imgJsonMap[curRemarkThumb];
    if (!data) {
        alert('组件信息缺失，无法添加图片');
        return;
    }
    var sources = [];
    for (var i = 0; i < fileList.length; i++) {
        if (fileList[i].path) {
            sources.push(fileList[i].path);
        }
    }
    var dests = copyRemarkImagesToCompDir(sources, getParentDir(data.json.thumbnailPath), data.json.layerName);
    curRemarkImages = curRemarkImages.concat(dests);
    renderRemarkImages();
    // 清空 input 值，允许再次选择相同文件
    $('#remarkImageInput').val('');
}

// 把源图片列表复制到组件目录，返回复制后的目标路径数组（用于编辑弹窗与新增面板）
function copyRemarkImagesToCompDir(sourcePaths, compDir, layerName) {
    var destPaths = [];
    // 图层名本身即可作为合法文件名（缩略图同规则），不能复用 getFileName（会按点截断无扩展名的名称）
    var baseName = layerName + '_remark';
    var idx = 0;
    for (var i = 0; i < sourcePaths.length; i++) {
        var src = sourcePaths[i];
        if (!src) {
            continue;
        }
        var ext = getExt(src);
        var dest = compDir + '/' + baseName + '_' + idx + '.' + ext;
        while (fs.existsSync(dest)) {
            idx++;
            dest = compDir + '/' + baseName + '_' + idx + '.' + ext;
        }
        try {
            fs.copyFileSync(src, dest);
            destPaths.push(dest);
            idx++;
        } catch (e) {
            alert('复制备注图片失败：' + src + '\n' + e);
        }
    }
    return destPaths;
}

// ===== 新增面板的备注输入 =====

// 读取新增面板的备注输入：把已选图片复制进组件目录，返回 {text, images}；无内容返回 null
function collectAddRemark(layerName, thumbnailPath) {
    var text = ($('#addRemarkText').val() || '').trim();
    var images = [];
    if (curAddRemarkImages.length > 0) {
        images = copyRemarkImagesToCompDir(curAddRemarkImages, getParentDir(thumbnailPath), layerName);
    }
    if (!text && images.length === 0) {
        return null;
    }
    return { text: text, images: images };
}

// 新增面板：选择备注图片（先记录源路径，新增成功时再复制进组件目录）
function addPanelRemarkImages(fileList) {
    if (!fileList || fileList.length === 0) {
        return;
    }
    for (var i = 0; i < fileList.length; i++) {
        if (fileList[i].path) {
            curAddRemarkImages.push(fileList[i].path);
        }
    }
    renderAddRemarkImages();
    $('#addPanelImageInput').val('');
}

// 新增面板：渲染备注图片缩略图列表（带删除按钮，点击可预览）
function renderAddRemarkImages() {
    var box = $('#addRemarkImages');
    box.empty();
    if (curAddRemarkImages.length === 0) {
        box.append('<div class="remark-empty">暂无图片，点击“添加图片”选择图片</div>');
        return;
    }
    for (var i = 0; i < curAddRemarkImages.length; i++) {
        (function (idx) {
            var url = curAddRemarkImages[idx];
            var item = $('<div class="remark-image-item"></div>');
            $('<img>').attr('src', url).on('click', function () {
                showPhotoViewer(curAddRemarkImages);
            }).appendTo(item);
            $('<button type="button" class="remark-image-del" title="删除">×</button>').on('click', function () {
                curAddRemarkImages.splice(idx, 1);
                renderAddRemarkImages();
            }).appendTo(item);
            box.append(item);
        })(i);
    }
}

// 新增面板：清空备注输入（打开弹窗/重置时调用）
function clearAddRemark() {
    curAddRemarkImages = [];
    $('#addRemarkText').val('');
    $('#addRemarkImages').empty();
}

// 转义 HTML 特殊字符，避免备注文本被当作标签解析
function escapeHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
