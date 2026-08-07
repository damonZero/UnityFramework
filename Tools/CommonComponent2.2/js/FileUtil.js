const fs = require('fs');

/**
 * @typedef {Object} jsonData
 * @property {string} psdPath - PSD 文件路径
 * @property {string} layerName - PSD 图层组名
 * @property {string} thumbnailPath - 缩略图文件路径
 * @property {string[]} tags - 标签数组
 */

const DEFUALT_TAGS = "默认标签.txt";
const CONFIG_TXT = "cfg.txt";

var rootPath;

var cfgPath;

//key:tag string  value: json[]
var tagJsonMap = {}

//key:img src value: {json,jsonPath}
var imgJsonMap = {}

var curSelect

var isInit = false

// 保存当前正在修改的组件的原始信息，用于修改时对比变化
var origModifyData = null

function initFileCfgPath(pluginPath) {
    try {
        const data = fs.readFileSync(`${pluginPath}/${CONFIG_TXT}`, 'utf8');
        rootPath = data.split("=")[1].replace(/\\/g, "/");
        // alert(rootPath);
        cfgPath = rootPath + "/pluginCfg";
         fs.readdir(cfgPath, (err, files) => {
            if (err) {
                console.error("无法读取文件夹:", err);
                return;
            }
        //    alert("文件列表:"+ files);
        });
        //初始化默认tags;
        initDefaultTags();
        initDefaultMenu();
    } catch (error) {
        alert(error)
    }
}

function getCfgPath() {
    return cfgPath;
}

function resetCash() {
    tagJsonMap = {};
    imgJsonMap = {};
    isInit = false;
    getJsonInfo('');
}


function getJsonInfo(query) {
    if (!isInit) {
        initFiles();
    }
    var results = [];

    if (!query.trim()) {
        return queryHandler();
    }

    var targets = query.trim().split(' ');

    for (let i = 0; i < targets.length; i++) {
        const target = targets[i].trim();
        if (target) {
            // alert(target);
            results.push(queryHandler(target));
        }
    }

    return intersection(...results);

}

//如果没有输入查询，默认是展示全部
function queryHandler(query) {
    const results = [];
    const regex = new RegExp(query, 'i'); // 创建正则表达式对象，忽略大小写

    var tags = Object.keys(tagJsonMap)

    for (let i = 0; i < tags.length; i++) {
        const tag = tags[i]
        // 使用正则表达式进行匹配
        if (query && !regex.test(tag)) {
            continue;
        }

        // alert(`match regex query : ${query} , tag : ${tag}`)

        const jsonArr = tagJsonMap[tag]
        for (let i = 0; i < jsonArr.length; i++) {
            const json = jsonArr[i];
            if (results.includes(json)) {
                continue;
            }
            results.push(json);
        }
    }

    // logJsonArr(results)

    return results;
}


function initFiles() {
    if (isInit) {
        return
    }

    try {
        //获取json文件路径
        const jsonFiles = getJsonFiles(cfgPath);
        //解析json信息
        parseJosnInof(jsonFiles);
        //检验
        // logJsonDict()
        isInit = true;
    } catch (error) {
        alert(error)
    }
}

function parseJosnInof(jsonFiles) {
    for (let i = 0; i < jsonFiles.length; i++) {
        const path = jsonFiles[i]
        const data = fs.readFileSync(path, 'utf8');
        jsonInofHandler(path, JSON.parse(data))
    }
}

function jsonInofHandler(filePath, jsonData) {
    /** @type {jsonData} */
    const d = jsonData

    d.psdPath = rootPath + d.psdPath;
    d.thumbnailPath = rootPath + d.thumbnailPath;

    // const psdPath = d.psdPath;
    const tags = d.tags;

    // img src - json map
    imgJsonMap[d.thumbnailPath] = {'json': d, 'jsonPath': filePath};

    // tag - json[] map
    addJsonMapInfo(getFileName(filePath), jsonData)
    for (let i = 0; i < tags.length; i++) {
        var tag = tags[i];
        addJsonMapInfo(tag, jsonData)
    }
}

function addJsonMapInfo(tag, json) {
    var jsonArr = tagJsonMap[tag]
    if (!jsonArr) {
        jsonArr = [json]
        tagJsonMap[tag] = jsonArr
        return
    }
    if (jsonArr.includes(json)) {
        return
    }
    jsonArr.push(json)
}

function getJsonFiles(dir, jsonFs = []) {
    const files = fs.readdirSync(dir);
    // alert('files.length ' + files.length)
    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        if (file.endsWith(DEFUALT_TAGS)) {
            continue;
        }
        const fullPath = dir + "/" + file;
        const stats = fs.statSync(fullPath);
        if (stats.isDirectory()) {
            getJsonFiles(fullPath, jsonFs);
        } else if (stats.isFile() && fullPath.endsWith('.json')) {
            jsonFs.push(fullPath);
        }
    }

    return jsonFs;
}

function addNewJson(psdPath, layerName, thumbnailPath, tags) {
    /** @type {jsonData} */
    var data = {
        'psdPath': psdPath,
        'thumbnailPath': thumbnailPath,
        'tags': tags.length > 0 ? tags.split(' ') : [],
        'layerName': layerName
    }
    return writeJsonHandler(data, true);
}

// 批量新增公共组件（静默模式：不逐个弹成功提示，成功后统一刷新）
function addNewJsons(items, tags) {
    var successCount = 0;
    for (var i = 0; i < items.length; i++) {
        var item = items[i];
        /** @type {jsonData} */
        var data = {
            'psdPath': item.psdPath,
            'thumbnailPath': item.thumbnailPath,
            'tags': tags.length > 0 ? tags.split(' ') : [],
            'layerName': item.layerName
        }
        if (writeJsonHandlerSilent(data)) {
            successCount++;
        }
    }
    resetCash();
    return successCount;
}

// 静默版写入：写入单个 JSON，不弹成功 alert（供批量调用）
function writeJsonHandlerSilent(jsonData) {
    var savePath;
    try {
        var parentDir = getParentDir(jsonData.thumbnailPath);
        // JSON 文件名直接用组件名，不再拼接 psd 序号
        savePath = parentDir + '/' + jsonData.layerName + '.json';

        let regex = new RegExp(rootPath, "i");
        jsonData.psdPath = jsonData.psdPath.replace(regex, "");
        jsonData.thumbnailPath = jsonData.thumbnailPath.replace(regex, "");

        var json = JSON.stringify(jsonData, null, 2);
    } catch (err) {
        alert(err);
        return false;
    }
    try {
        fs.writeFileSync(savePath, json);
    } catch (err) {
        alert(err);
        return false;
    }
    return true;
}

function writeJsonHandler(jsonData, isNew, oldJsonPath) {
    var savePath;
    try {
        var thumbnailPath = jsonData.thumbnailPath

        var parentDir = getParentDir(thumbnailPath);

        // JSON 文件名直接用组件名，不再拼接 psd 序号
        savePath = parentDir + '/' + jsonData.layerName + '.json';

        let regex = new RegExp(rootPath, "i");
        jsonData.psdPath = jsonData.psdPath.replace(regex, "");
        jsonData.thumbnailPath = jsonData.thumbnailPath.replace(regex, "");

        var json = JSON.stringify(jsonData, null, 2);
    } catch (err) {
        alert(err);
        return false;
    }
    try {
        fs.writeFileSync(savePath, json);

        // 当组件名发生变化时，旧 JSON 文件路径与新路径不同，需要删除旧文件
        if (oldJsonPath) {
            var normalizedOld = oldJsonPath.replace(/\\/g, '/');
            if (normalizedOld !== savePath) {
                fs.unlinkSync(oldJsonPath);
            }
        }
    } catch (err) {
        alert(err);
        return false;
    }
    alert(isNew ? "创建或替换成功" : "修改成功");

    if (isNew) {
        showChooseThumbnail(thumbnailPath);
    }
    //重新更新查询数据
    resetCash();
    return true;
}

function showModifyInfo(thumbnailPath) {
    var data = imgJsonMap[thumbnailPath];
    /** @type {jsonData} */
    var json = data.json;
    if (!json) {
        alert('没有找到配置文件');
        return;
    }

    // 保存原始数据，用于修改时检测变化（删旧文件、重命名等）
    origModifyData = {
        thumbnailPath: json.thumbnailPath,
        layerName: json.layerName,
        jsonPath: data.jsonPath,
        psdPath: json.psdPath
    };

    $('#showModifyName').text(json.layerName);
    $('#modifyPsdPath').val(json.psdPath);
    $('#modifyThumbnail').val(json.thumbnailPath);
    $('#modifyTags').val(json.tags.join(" "));
    $('#modifyLayerName').val(json.layerName);

    updateInputWidth();
}

function saveModifyJson() {
    var flag = confirm("确定修改么？");
    if (!flag) {
        return;
    }

    if (!origModifyData) {
        alert('原始数据丢失，请关闭对话框后重新选择组件');
        return;
    }

    var newLayerName = $('#modifyLayerName').val().trim();
    var tags = $('#modifyTags').val().trim();
    var newPsdPath = $('#modifyPsdPath').val().trim();

    var origThumbnailPath = origModifyData.thumbnailPath;
    var origLayerName = origModifyData.layerName;
    var origJsonPath = origModifyData.jsonPath;

    var nameChanged = (newLayerName !== origLayerName);
    var newThumbnailPath = origThumbnailPath;

    if (nameChanged) {
        // 重命名缩略图 PNG，使其与新的组件名保持一致
        var parentDir = getParentDir(origThumbnailPath);
        newThumbnailPath = parentDir + '/' + newLayerName + '.png';
        try {
            fs.renameSync(origThumbnailPath, newThumbnailPath);
        } catch (err) {
            alert('重命名缩略图失败: ' + err);
            return;
        }
    }

    /** @type {jsonData} */
    var data = {
        'psdPath': newPsdPath,
        'thumbnailPath': newThumbnailPath,
        'tags': tags.length > 0 ? tags.split(' ') : [],
        'layerName': newLayerName
    };

    var success = writeJsonHandler(data, false, origJsonPath);
    if (!success) {
        return;
    }

    // 如果组件名称发生变化，同步修改 PSD 文件中对应组的名字
    if (newLayerName !== origLayerName) {
        var encParams = encodeURI(newPsdPath + '@' + origLayerName + '@' + newLayerName);
        CSLibrary.evalScript('renameLayerInPsd("' + encParams + '")', function (result) {
            if (result && result.indexOf('error') >= 0) {
                alert('PSD图层重命名失败: ' + result);
            }
            updateThumbnail();
            layer.closeAll();
        });
    } else {
        updateThumbnail();
        layer.closeAll();
    }
}

function replaceJson() {
    var flag = confirm("确定替换么？请确保在PSD中已经选中需要更新的组！！！");
    if (!flag) {
        return;
    }
    delJson(true);
    addJsonCfg(true);
    updateThumbnail();
}

function delJson(isReplace) {
    if (!isReplace) {
        var flag = confirm("确定删除么？");
        if (!flag) {
            return;
        }
    }
    var thumbnailPath = $('#modifyThumbnail').val();
    var data = imgJsonMap[thumbnailPath];
    if (!data) {
        alert('不存在该文件：' + thumbnailPath);
    }
    var jsonPath = data.jsonPath;

    try {
        fs.unlinkSync(thumbnailPath);
        // alert('文件已成功删除：' + thumbnailPath);
        fs.unlinkSync(jsonPath);
        alert('文件已成功删除：' + jsonPath);

    } catch (err) {
        alert(err);
    }

    if (isReplace) {
        return;
    }

    resetCash();
    layer.closeAll();
    updateThumbnail();
}


function initDefaultTags() {
    try {
        const data = fs.readFileSync(cfgPath + "/" + DEFUALT_TAGS, 'utf8');
        var tags = data.split('|');
        var mySelect = $('#selectTag');
        mySelect.empty();
        mySelect.append($('<option>', {
            value: '',
            text: "全部"
        }));
        for (let i = 0; i < tags.length; i++) {
            const tag = tags[i];
            mySelect.append($('<option>', {
                value: tag,
                text: tag
            }));
        }

    } catch (error) {
        alert(error)
    }
}

function initDefaultMenu() {
    try {
        const data = fs.readFileSync(cfgPath + "/" + DEFUALT_TAGS, 'utf8');
        var tags = data.split('|');
        var menus = $('#menu');
        menus.empty();
        menus.append('<li class="layui-nav-item layui-this menus"><a href=javascript:;>全部</a></li>');
        for (let i = 0; i < tags.length; i++) {
            const tag = tags[i];
            menus.append('<li class="layui-nav-item menus"><a href="javascript:;">' + tag + '</a></li>');
        }

        layui.use('element', function () {
            var element = layui.element;
            var layFilter = $("#menu").attr('lay-filter');
            element.render('nav', layFilter);
        })

        var itemli = document.getElementsByClassName("menus");

        for (var i = 0; i < itemli.length; i++) {

            itemli[i].index = i; //给每个li定义一个属性索引值
            itemli[i].onclick = function () {
                // 获取 a 元素
                var aElement = this.querySelector('a');
                // 获取文本内容
                var text = aElement.textContent;
                if (text == '全部') {
                    text = ''
                }
                setCurMenuVal(text);
            }

        }
        var allTab = document.querySelector("#menu li:first-child");
        allTab.click();
    } catch (error) {
        alert(error)
    }
}


function logJsonDict() {
    alert("jsonDict.length" + Object.keys(tagJsonMap).length)
    for (const key in tagJsonMap) {
        alert(`json key : ${key}`);
        const jsonArr = tagJsonMap[key]
        logJsonArr(jsonArr)
    }
}

function logJsonArr(jsonArr) {
    alert(`jsonArr.length : ${jsonArr.length}`)
    for (let i = 0; i < jsonArr.length; i++) {
        /** @type {jsonData} */
        const json = jsonArr[i];
        alert(`psdPath : ${json.psdPath}`);
        alert(`layerName : ${json.layerName}`);
        alert(`thumbnailPath : ${json.thumbnailPath}`);
        alert(`tags : ${json.tags}`);
    }
}

function getParentDir(filePath) {
    var parentDir = filePath.replace(/\\/g, "/") // 将反斜杠替换为斜杠
        .replace(/\/[^\/]*$/, ""); // 匹配最后一个斜
    return parentDir
}

function getFileName(filePath) {
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

function intersection(...arrays) {
    let set = new Set(arrays[0]);
    for (let i = 1; i < arrays.length; i++) {
        set = new Set(arrays[i].filter(element => set.has(element)));
    }
    return Array.from(set);
}