var doc = app.activeDocument
var textHint = "";
var imageHint = "";
var ndHint = "";
adjustLayers(doc.layers, 0); //遍历检查图层
printResult(); //出处结果

//遍历图层
function adjustLayers(layers, count) {
    for (var i = 0; i < layers.length; i++) {
        //判断是否是图层组
        if (layers[i].typename == "LayerSet") {
            //递归
            adjustLayers(layers[i].layers, count + 1);
        } else {
            var layer = layers[i];
            var kind = layer.kind;
            //名字校正
            //图片
            if (kind == LayerKind.NORMAL) {
                adjustName(layer, -1);
                adjustImage(layer);
            }
            //文字
            if (kind == LayerKind.TEXT) {
                adjustName(layer, 64);
                adjustText(layer);
            }
        }
    }
}

//输出结果
function printResult() {
    var resultHint = "";
    if (textHint != "") {
        resultHint += "========文本错误提示【必须处理!!!】========";
        resultHint += textHint;
    }
    if (imageHint != "") {
        resultHint += "========图片警告提示========";
        resultHint += imageHint;
    }
    if (resultHint != "")
        alert(resultHint);
    else
        alert("检查结束!");
}

//校正名字
function adjustName(layer, excludeUni) {
    //替换空格
    var name = layer.name;
    name = name.replace(" ", "");
    //特殊字符转换
    var nameArray = new Array(name.length);
    for (i = 0; i < name.length; i++) {
        var nameChar = name.charCodeAt(i);
        nameArray[i] = name[i];
        //中文ps中不处理,方便美术查看,在Unity中转换为英文
        if (nameChar <= 255) {
            //只保留大小写字母、数字、_，其他字符转'_'
            if (!((nameChar == excludeUni) ||
                    (nameChar >= 48 && nameChar <= 57) ||
                    (nameChar >= 65 && nameChar <= 90) ||
                    (nameChar >= 97 && nameChar <= 122))) {
                nameArray[i] = '_';
            }
        }
    }
    name = nameArray.join("");
    layer.name = name;
}

//文字处理
function adjustText(layer) {
    //获得文本尺寸
    var textSize = getTextSize(layer).toFixed(0);
    //解析文本编号,判断是否需要添加文本尺寸
    var params = layer.name.split("@");
    var addSize = false;
    if (params.length > 1) {
        //最后一个不是尺寸数字时,需要添加数字
        var sizeNum = parseInt(params[params.length - 1]);
        if (isNaN(sizeNum)) {
            addSize = true;
        } else {
            //最后一个是数字则更新
            params[params.length - 1] = textSize
            layer.name = params.join("");
        }
        //倒数第二个不是文本属性编号时,给出提示
        var propertyNum = parseInt(params[params.length - 2]);
        if (isNaN(propertyNum)) {
            textHint += "\n[文本]: '" + layer.name + "' 没有属性编号";
            alert(propertyNum + "===" + params[params.length - 2]);
        }
    } else {
        addSize = true;
        //命名不是文本属性编号时,需要加数字
        var propertyNum = parseInt(layer.name);
        if (isNaN(propertyNum)) {
            textHint += "\n[文本]: '" + layer.name + "' 没有属性编号";
        }
    }
    //是否需要添加文本尺寸
    if (addSize) {
        layer.name += "@" + textSize;
    }
}

//获取文字尺寸
function getTextSize(layer) {
    var r = new ActionReference();
    r.putProperty(stringIDToTypeID("property"), stringIDToTypeID("textKey"));
    r.putIdentifier(stringIDToTypeID("layer"), layer.id);
    return executeActionGet(r).getObjectValue(stringIDToTypeID("textKey")).getList(stringIDToTypeID('textStyleRange'))
        .getObjectValue(0).getObjectValue(stringIDToTypeID('textStyle')).getUnitDoubleValue(stringIDToTypeID('impliedFontSize'))
}

//处理图片
function adjustImage(layer) {
    //图层命名检查
    var name = layer.name;
    var start3 = name.substr(0, 3).toLowerCase();
    var start4 = name.substr(0, 4).toLowerCase();
    var start5 = name.substr(0, 5).toLowerCase();
    if (!(start4 == "img_" || start4 == "btn_" || start4 == "ctn_" ||
            start5 == "icon_" || start3 == "bg_" || start3 == "nd_")) {
        imageHint += "\n图片: '" + layer.name + "' 命名不标准";
    }
    //2次智能化图层
    if (!layer.kind.SMARTOBJECT) {
        // alert("智能化图层:" + layer.name);
        // //图层有链接时
        // if(){

        // }
        // //图层下有效果时
        // if(){

        // }
        // smartObjectTwice(layer);
    }
}

//两次智能化图层(只智能化一次,导出到Unity异常)
function smartObjectTwice(layer) {
    var idslct = charIDToTypeID("slct");
    var desc9 = new ActionDescriptor();
    var idnull = charIDToTypeID("null");
    var ref4 = new ActionReference();
    var idLyr = charIDToTypeID("Lyr ");
    ref4.putName(idLyr, layer.name);
    desc9.putReference(idnull, ref4);
    var idMkVs = charIDToTypeID("MkVs");
    desc9.putBoolean(idMkVs, false);
    var idLyrI = charIDToTypeID("LyrI");
    var list1 = new ActionList();
    list1.putInteger(4);
    desc9.putList(idLyrI, list1);
    executeAction(idslct, desc9);
    var idnewPlacedLayer = stringIDToTypeID("newPlacedLayer");
    executeAction(idnewPlacedLayer, undefined, DialogModes.NO);
}

//获取链接图层
function getLinkedLayers() {
    var idlinkSelectedLayers = stringIDToTypeID("linkSelectedLayers");
    var desc929 = new ActionDescriptor();
    var idnull = charIDToTypeID("null");
    var ref617 = new ActionReference();
    var idLyr = charIDToTypeID("Lyr ");
    var idOrdn = charIDToTypeID("Ordn");
    var idTrgt = charIDToTypeID("Trgt");
    ref617.putEnumerated(idLyr, idOrdn, idTrgt);
    desc929.putReference(idnull, ref617);
    executeAction(idlinkSelectedLayers, desc929, DialogModes.NO);
}