
const eventJson = 'eventJson'

var sizeMap = {}

var curMenuVal = ""

var curTabColor = "#c2c2c2"

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

    // updateThumbnail();
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
        showModifyInfo(img.attr('src'));
        onModifyBtn();
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
