var cfgPath;

var getCurLayerInfo = function (cfgPath) {
    try {
        var currentDocument = app.activeDocument;
        var currentFilePath = decodeURI(currentDocument.fullName);
        var psdPath = currentFilePath.replace(/^\/(.)/, "$1:");

        var activeLayer = currentDocument.activeLayer;
        var layerName = activeLayer.name;

        var imgDirPath = cfgPath + "/" + getFileName(psdPath)
        var thumbnailPath = createImg(imgDirPath);
        if (!thumbnailPath) {
            // createImg 内部已 alert 具体原因，这里直接终止，避免把 undefined 拼进 JSON 路径
            return '';
        }

        return encodeURI(psdPath + "@" + layerName + "@" + thumbnailPath);
    } catch (e) {
        alert("获取图层信息失败: " + e);
        return '';
    }
}

var createImg = function (imgDirPath) {
    var currentDocument = app.activeDocument;
    var activeLayer = currentDocument.activeLayer;

    // 规定：只有图层组(LayerSet)才能作为公共组件添加
    if (activeLayer.typename !== "LayerSet") {
        alert("请选择图层组(组)，不能直接选择普通图层或背景层");
        return;
    }

    var layer = activeLayer

    // 注意：PSD 的 bounds 顺序是 [Top, Left, Bottom, Right]
    // 新建一个画布，尺寸用组 bounds 的宽高并各留 200px 安全余量，
    // 防止组内阴影/发光等实际内容略超出组边界；复制归位后再 trim 掉透明边缘贴合真实像素

    var gBounds = layer.bounds;
    var gWidth = gBounds[3].as("px") - gBounds[1].as("px");
    var gHeight = gBounds[2].as("px") - gBounds[0].as("px");

    var newDoc = app.documents.add(gWidth + 200, gHeight + 200, 72, "New Document", NewDocumentMode.RGB, DocumentFill.TRANSPARENT);
    app.activeDocument = currentDocument;

    var dupLayer = layer.duplicate(newDoc, ElementPlacement.INSIDE);
    app.activeDocument = newDoc;

    // 将复制进来的图层移到新画布左上角(0,0)附近（留 100px 边距）
    var dupTop = dupLayer.bounds[0].as("px");
    var dupLeft = dupLayer.bounds[1].as("px");
    dupLayer.translate(-dupLeft + 100, -dupTop + 100);

    // 裁剪掉四周透明像素，让画布精确贴合内容真实像素边界，避免边缘裁切
    // 用 try-catch 包裹：即使 trim 失败也继续导出，不中断缩略图生成
    try {
        newDoc.trim(TrimType.TRANSPARENT);
    } catch (trimErr) {
        alert("裁剪透明边缘失败(忽略继续): " + trimErr);
    }

    var exportOptions = new ExportOptionsSaveForWeb();
    exportOptions.format = SaveDocumentType.PNG;
    exportOptions.PNG8 = false;
    exportOptions.transparency = true;
    exportOptions.interlaced = false;
    exportOptions.quality = 100;
    exportOptions.includeProfile = false;
    exportOptions.optimized = true;

    // alert(imgDirPath)
    createFolder(imgDirPath);

    // 缩略图文件名直接用组件名，不再拼接 psd 序号
    var thunbnailPath = imgDirPath + "/" + layer.name + ".png"
    var file = new File(thunbnailPath);

    newDoc.exportDocument(file, ExportType.SAVEFORWEB, exportOptions);

    newDoc.close(SaveOptions.DONOTSAVECHANGES)

    return thunbnailPath;
}

var addPsdHandler = function (params) {

    var infos = decodeURI(params).split('&');

    var path = infos[0];

    if (app.documents.length == 0) {
        openPsdHandler(path);
        return;
    }

    // 参数格式: path&layerName （组件按名称查找，不再依赖 psd 序号）
    var layerName = infos[1] || "";

    var docRef = app.activeDocument;
    var importedDocRef = null;

    try {
        var fileRef = new File(path);
        if (!fileRef.exists) {
            alert("[addPsd] 文件不存在: " + path);
            return;
        }

        importedDocRef = app.open(fileRef);

        var layer = null;

        // 按名称查找图层（递归搜索整棵图层树，不再要求顶层是包装组）
        if (layerName) {
            layer = findLayerByName(importedDocRef, layerName);
        }

        if (!layer) {
            alert("[addPsd] 错误: 未找到图层\n名称: " + layerName);
            importedDocRef.close(SaveOptions.DONOTSAVECHANGES);
            app.activeDocument = docRef;
            return;
        }

        // 规定：只有图层组(LayerSet)才能作为公共组件添加
        if (layer.typename !== "LayerSet") {
            alert("[addPsd] 错误: '" + layer.name + "' 不是图层组(LayerSet)，公共组件必须是组\n实际类型: " + layer.typename);
            importedDocRef.close(SaveOptions.DONOTSAVECHANGES);
            app.activeDocument = docRef;
            return;
        }

        app.activeDocument = importedDocRef;
        layer.duplicate(docRef, ElementPlacement.PLACEATBEGINNING);

        importedDocRef.close(SaveOptions.DONOTSAVECHANGES);
        app.activeDocument = docRef;

    } catch (e) {
        alert("[addPsd] 异常: " + e.message + "\npath: " + path + "\nlayerName: " + layerName);
        try {
            if (importedDocRef) {
                importedDocRef.close(SaveOptions.DONOTSAVECHANGES);
            }
            app.activeDocument = docRef;
        } catch (e2) {}
    }
}

var newPsdHandler = function () {
    // 创建一个新文档
    var docRef = app.documents.add(800, 600, 72, "My New Document");
    // 在控制台输出文档信息
    alert("已创建文档：" + docRef.name + "\n宽度：" + docRef.width + " 像素\n高度：" + docRef.height + " 像素");
}

// var openPsdHandler = function (path) {
//     // alert('openPsdHandler psd :' + path)
//     var fileRef = new File(path);
//     var docRef = app.open(fileRef);
//     // 在控制台输出文档信息
//     alert("已打开文档：" + docRef.name + "\n宽度：" + docRef.width + " 像素\n高度：" + docRef.height + " 像素");
// }

var createFolder = function (path) {
    var folder = new Folder(path);
    if (!folder.exists) {
        var parentFolder = new Folder(folder.parent);
        createFolder(parentFolder);
        folder.create();
    }
}

// 递归在文档/容器中按名称查找图层（Layer 或 LayerSet），找不到返回 null
var findLayerByName = function (container, name) {
    var layers = container.layers;
    for (var i = 0; i < layers.length; i++) {
        var l = layers[i];
        if (l.name === name) {
            return l;
        }
        // 是图层组则深入查找
        if (l.typename === "LayerSet") {
            var found = findLayerByName(l, name);
            if (found) {
                return found;
            }
        }
    }
    return null;
}

var getFileName = function (filePath) {
    // 从右边查找第一个"/"的位置
    const lastSlashIndex = filePath.lastIndexOf("/");

    // 从最后一个"/"后面的位置开始提取文件名
    const fileNameWithExtension = filePath.slice(lastSlashIndex + 1);

    // 查找文件名中第一个"."的位置
    const dotIndex = fileNameWithExtension.indexOf(".");

    // 提取不包括扩展名的文件名部分
    const fileName = fileNameWithExtension.slice(0, dotIndex);

    return fileName
}

// 在指定PSD文件中将顶层组内的子图层从 oldName 重命名为 newName，并保存文件
// params 格式（URL编码）: psdPath@oldName@newName
var renameLayerInPsd = function (params) {
    var parts = decodeURI(params).split('@');
    var psdPath = parts[0];
    var oldName = parts[1];
    var newName = parts[2];

    // 处理路径格式：Photoshop 内部路径可能带前导斜杠，如 /E:/...
    psdPath = psdPath.replace(/^\/(.)/, '$1:');

    var fileRef = new File(psdPath);
    if (!fileRef.exists) {
        return 'error: file not exists - ' + psdPath;
    }

    // 如果 PSD 已经在 Photoshop 中打开，则复用现有引用，避免打开副本或意外关闭用户文档
    var docRef = null;
    var wasAlreadyOpen = false;
    for (var d = 0; d < app.documents.length; d++) {
        try {
            var openedPath = decodeURI(app.documents[d].fullName).replace(/^\/(.)/, '$1:');
            if (openedPath.toLowerCase() === psdPath.toLowerCase()) {
                docRef = app.documents[d];
                wasAlreadyOpen = true;
                break;
            }
        } catch (e2) {}
    }

    if (!docRef) {
        docRef = app.open(fileRef);
    }

    try {
        // 递归搜索整棵图层树，不再要求组件一定在顶层包装组(layers[0])内
        var found = false;
        var target = findLayerByName(docRef, oldName);
        if (target) {
            target.name = newName;
            found = true;
        }

        if (!found) {
            if (!wasAlreadyOpen) {
                docRef.close(SaveOptions.DONOTSAVECHANGES);
            }
            return 'error: layer "' + oldName + '" not found';
        }

        docRef.save();
        if (!wasAlreadyOpen) {
            docRef.close(SaveOptions.DONOTSAVECHANGES);
        }
        return 'success';
    } catch (e) {
        if (!wasAlreadyOpen) {
            try { docRef.close(SaveOptions.DONOTSAVECHANGES); } catch (e3) {}
        }
        return 'error: ' + e;
    }
}