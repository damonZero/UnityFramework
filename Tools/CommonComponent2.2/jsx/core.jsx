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

// 批量获取当前选中的多个图层组信息
// 返回格式: 组件组信息用 ~ 连接成一行，整体用 encodeURI 编码。
// 每个组件: psdPath@layerName@thumbnailPath
// 单选时返回单行（不含 ~），多选时返回多行。
// 要求：选中的图层必须全部是图层组(LayerSet)，否则返回错误提示并中止
var getCurLayersInfo = function (cfgPath) {
    try {
        var currentDocument = app.activeDocument;
        var currentFilePath = decodeURI(currentDocument.fullName);
        var psdPath = currentFilePath.replace(/^\/(.)/, "$1:");

        var imgDirPath = cfgPath + "/" + getFileName(psdPath)

        // 读取当前选中的多个图层
        var selectedLayers = getSelectedLayers();
        if (!selectedLayers || selectedLayers.length == 0) {
            alert("未选中任何图层，请先选中需要添加的图层组");
            return '';
        }

        // 校验：必须全部是图层组(LayerSet)
        for (var i = 0; i < selectedLayers.length; i++) {
            if (selectedLayers[i].typename !== "LayerSet") {
                alert("批量添加要求选中的图层必须全部是组，\n其中 '" + selectedLayers[i].name + "' 不是图层组(LayerSet)，已中止。\n请重新只选择图层组。");
                return '';
            }
        }

        // 逐个生成缩略图并收集信息
        var lines = [];
        for (var j = 0; j < selectedLayers.length; j++) {
            var l = selectedLayers[j];
            // createImg 第二个参数传入指定图层，避免依赖“活动图层”状态在批量时被串扰
            var thumPath = createImg(imgDirPath, l);
            if (!thumPath) {
                continue;
            }
            lines.push(psdPath + "@" + l.name + "@" + thumPath);
        }

        if (lines.length == 0) {
            alert("没有成功生成任何组件缩略图");
            return '';
        }

        return encodeURI(lines.join("~"));
    } catch (e) {
        alert("批量获取图层信息失败: " + e);
        return '';
    }
}

// 读取当前选中的多个图层（含嵌套组内的图层）
// 社区最广泛验证的方案：用 Action Manager 把当前选中图层临时打包成组，
// 读取组内子层（即之前选中的图层），再 undo 撤销建组恢复原状。
// 该方案在 Photoshop CS4 到 2020 均验证可靠，且无需处理索引偏移。
var getSelectedLayers = function () {
    var selectedLayers = [];
    var doc = app.activeDocument;

    try {
        // 新建组：把当前选中的图层(target)作为组的来源
        var desc = new ActionDescriptor();
        var ref = new ActionReference();
        ref.putClass(stringIDToTypeID("layerSection")); // 新组类型
        desc.putReference(charIDToTypeID("null"), ref);
        var lref = new ActionReference();
        lref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt")); // 目标=选中图层
        desc.putReference(charIDToTypeID("From"), lref);
        executeAction(charIDToTypeID("Mk  "), desc, DialogModes.NO);

        // 建组后，新组成为活动图层；读取其子层（即之前选中的图层）
        var group = doc.activeLayer;
        if (group && group.typename === "LayerSet") {
            var childLayers = group.layers;
            for (var i = 0; i < childLayers.length; i++) {
                selectedLayers.push(childLayers[i]);
            }
        }

        // 撤销建组，恢复原始图层结构（图层引用在 undo 后仍有效，社区已验证）
        executeAction(charIDToTypeID("undo"), undefined, DialogModes.NO);
    } catch (e) {
        // 建组/撤销失败时，回退到单数 activeLayer（单选必可靠）
        selectedLayers = [];
        try {
            var al = doc.activeLayer;
            if (al) {
                selectedLayers.push(al);
            }
        } catch (e3) {}
    }

    return selectedLayers;
}

var createImg = function (imgDirPath, targetLayer) {
    var currentDocument = app.activeDocument;
    // 若传入了指定图层(批量用)则优先使用它，否则读当前活动图层
    var activeLayer = targetLayer ? targetLayer : currentDocument.activeLayer;

    // 规定：只有图层组(LayerSet)才能作为公共组件添加
    if (activeLayer.typename !== "LayerSet") {
        alert("请选择图层组(组)，不能直接选择普通图层或背景层");
        return;
    }

    var layer = activeLayer

    // 注意：PSD 的 bounds 数组是 [Left, Top, Right, Bottom]，
    // 但不同版本/对象可能顺序有差异，这里用 min/max 组合算出与顺序无关的 left/top。
    // 直接按组的边界框(bounds)截取整个组作为缩略图，
    // 画布尺寸 = 组 bounds 的宽高，不做余量、不 trim，组内元素是否完整由美术保证
    var gBounds = layer.bounds;
    var g0 = gBounds[0].as("px"), g1 = gBounds[1].as("px"), g2 = gBounds[2].as("px"), g3 = gBounds[3].as("px");
    var gLeft = Math.min(g0, g2);   // left = 两个 X 坐标的较小者
    var gTop = Math.min(g1, g3);    // top = 两个 Y 坐标的较小者
    var gRight = Math.max(g0, g2);
    var gBottom = Math.max(g1, g3);
    var gWidth = gRight - gLeft;
    var gHeight = gBottom - gTop;

    // 保护：组边界框退化(宽高<=0)时无法生成缩略图
    if (gWidth <= 0 || gHeight <= 0) {
        alert("组件 '" + layer.name + "' 的边界框尺寸无效(" + gWidth + "x" + gHeight + ")，跳过");
        return;
    }

    var newDoc = app.documents.add(gWidth, gHeight, 72, "New Document", NewDocumentMode.RGB, DocumentFill.TRANSPARENT);
    app.activeDocument = currentDocument;

    var dupLayer = layer.duplicate(newDoc, ElementPlacement.INSIDE);
    app.activeDocument = newDoc;

    // 将复制进来的图层移到新画布原点(0,0)，让组边界框与画布对齐
    // 同样用 min/max 组合读取，避免 bounds 顺序差异导致偏移错误
    var d0 = dupLayer.bounds[0].as("px"), d1 = dupLayer.bounds[1].as("px"), d2 = dupLayer.bounds[2].as("px"), d3 = dupLayer.bounds[3].as("px");
    var dupLeft = Math.min(d0, d2);
    var dupTop = Math.min(d1, d3);
    dupLayer.translate(-dupLeft, -dupTop);

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