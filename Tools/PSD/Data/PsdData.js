/**
 * @desc Psd数据
 * @author shenhz
 */

const NODE_TYPE = {
    BASE: 0,
    ROOT: 1,
    GROUP: 2,
    IMAGE: 3,
    TEXT: 4,
    TEMPLATE: 5
};

const TARGET_HEIGHT = 1624;
const TARGET_WIDTH = 750;

const TOP_ALIGNMENT = {
    left: 'TopLeft',
    center: 'Top',
    right: 'TopRight'
};

const MIDDLE_ALIGNMENT = {
    left: 'Left',
    center: 'Center',
    right: 'Right'
};

const BOTTOM_ALIGNMENT = {
    left: 'BottomLeft',
    center: 'Bottom',
    right: 'BottomRight'
};

class PsdNode {
    constructor(nodeData, resourceSize, parent) {
        this.nodeData = nodeData;
        /**
         * 父节点
         * @type {PsdNode}
         */
        this.parent = parent;
        /**
         * @type {string}
         */
        this.name = nodeData.name.trim();
        this.type = NODE_TYPE.BASE;
        this.pos = [];
        this.size = [];
        /**
         * 子节点
         * @type {PsdNode[]}
         */
        this.childNodes = [];
        this.resourceSize = resourceSize;
    }

    /**
     * 子节点位置调换
     */
    childReverse() {
        let len = this.childNodes.length;

        if (len < 1)
            return;
        for (let i = len - 1; i >= 0; i--) {
            this.childNodes[i].childReverse();
        }

        if (len < 2)
            return;
        this.childNodes.reverse();
    }

    /**
     * 获取导出信息
     * @return {*}
     */
    getOutputInfo() {
        return {
            name: this.name,
            type: this.type,
            pos: this.pos,
            size: this.size
        };
    }

    /**
     * 数据准备，部分节点需要用到
     */
    prepare() {
        // 获取范围大小
        this.size = [this.nodeData.width, this.nodeData.height];

        // 获取位置
        this.pos = [this.nodeData.coords.left - this.resourceSize[0] / 2 + this.size[0] / 2,
            this.resourceSize[1] / 2 - (this.nodeData.coords.top + this.size[1] / 2)];

        this.childNodes.forEach(child => child.prepare());
    }

    /**
     * 后处理
     */
    afterAction() {
        // 子先处理，然后再处理父
        let len = this.childNodes.length;
        for (let i = len - 1; i >= 0; i--) {
            this.childNodes[i].afterAction();
        }

        PsdNode.fixFloatList(this.pos);
        PsdNode.fixFloatList(this.size);
    }

    /**
     * 删除子节点
     * @param child : PsdNode
     */
    deleteChild(child) {
        let index = this.childNodes.indexOf(child);
        if (index !== -1) {
            this.childNodes.splice(index, 1);
        }
    }

    /**
     * 修复浮点数列表，浮点数的话，仅保留两位小数
     * @param list : Array
     */
    static fixFloatList(list) {
        list.forEach((value, index) => {
            if (parseInt(value, 10) !== value) {
                list[index] = parseFloat(value.toFixed(2));
            }
        });
    }
}

class PsdGroup extends PsdNode {
    constructor(nodeData, resourceSize, parent) {
        super(nodeData, resourceSize, parent);
        this.type = NODE_TYPE.GROUP;
    }

    afterAction() {
        super.afterAction();
        // 如果没有子，那就不需要了
        // if (this.childNodes.length === 0) {
        //     this.parent.deleteChild(this);
        // }
    }

    /**
     * 获取导出信息
     * @override
     * @return {{name: string, childNodes: Array}}
     */
    getOutputInfo() {
        let result = [];
        this.childNodes.forEach(child => result.push(child.getOutputInfo()));
        return {
            name: this.name,
            type: this.type,
            pos: this.pos,
            size: this.size,
            childNodes: result
        }
    }
}

class PsdRootNode extends PsdGroup {
    constructor(psdName, resourceSize) {
        super({
            name: psdName
        }, resourceSize);
        this.type = NODE_TYPE.ROOT;
    }

    prepare() {
        this.pos = [0, 0];
        this.size = [TARGET_WIDTH, TARGET_HEIGHT];
        this.childNodes.forEach(child => child.prepare());
    }
}

class PsdImage extends PsdNode {
    constructor(nodeData, resourceSize, parent) {
        super(nodeData, resourceSize, parent);
        this.type = NODE_TYPE.IMAGE;
        // 图层不透明度(归一化 0-1, 1=完全不透明)
        this.opacity = nodeData.layer.opacity / 255;
    }

    afterAction() {
        // 如果图片名带中文，不给导出
        // if (this.name.match(/([^a-zA-Z0-9_-])/)) {
        // 	this.parent.deleteChild(this);
        // }
    }

    getOutputInfo() {
        return {
            name: this.name,
            type: this.type,
            pos: this.pos,
            size: this.size,
            opacity: this.opacity
        }
    }
}

class PsdTemplate extends PsdImage {
    constructor(nodeData, resourceSize, parent) {
        super(nodeData, resourceSize, parent);
        this.type = NODE_TYPE.TEMPLATE;
        this.templateName = undefined;
    }

    prepare() {
        super.prepare();
        this.templateName = this.name;
    }

    getOutputInfo() {
        return {
            name: this.name,
            type: this.type,
            pos: this.pos,
            size: this.size,
            templateName: this.templateName
        }
    }
}

class PsdText extends PsdNode {
    constructor(nodeData, resourceSize, parent) {
        super(nodeData, resourceSize, parent);
        this.type = NODE_TYPE.TEXT;
        this.content = '';
        this.font = undefined;
        this.fontSize = 0;
        this.color = undefined;
        this.italic = false;
        this.bold = false;
        this.underline = false;
        this.alignment = undefined;
        this.lineSpace = 0;
        this.letterSpacing = 0;
        // 分段颜色 [[r,g,b,a], ...](0-255) 与分段长度
        this.colorRuns = [];
        this.runLengths = [];
    }

    prepare() {
        super.prepare();

        const typeTool = this.nodeData.get('typeTool');
        const {transform, font, value} = typeTool.export();
        const {yy} = transform;
        const {Leading, Tracking, FauxItalic, FauxBold, Underline} = typeTool.styles();

        // 文本内容
        this.content = value;

        // 获取字体
        /**
         * @type string
         */
        const fontFamily = (font.names[0] || '').replace(/\s|\0/g, '');

        // 特殊的判断规则，A字体在项目内是加粗的
        this.font = fontFamily.includes('_CU') ? 'A' : 'B';
        // 获取字号
        this.fontSize = Math.round(font.sizes[0] * yy);
        // 行间距
        this.lineSpace = Leading && !isNaN(Number(Leading[0])) ? Leading[0] : 0;
        // 字间距
        // this.letterSpacing = Tracking ? Math.round(Tracking[0] * this.fontSize / 1000) : 0;
        this.letterSpacing = Tracking ? Tracking[0] : 0;
        // 获取颜色
        this.color = font.colors && font.colors.length ? PsdText.rgbToOne(font.colors[0]) : [1, 1, 1, 1];
        // 分段颜色(每段一个 RGBA, 0-255) 与分段长度
        this.colorRuns = font.colors;
        this.runLengths = font.lengthArray;
        // 是否斜体
        this.italic = FauxItalic ? FauxItalic[0] : false;
        // 是否加粗
        this.bold = FauxBold ? FauxBold[0] : false;
        // 下划线
        this.underline = Underline ? Underline[0] : false;
        // 对齐方式
        let [alignment = 'left', vAlignment = 'top'] = font.alignment || [];
        switch (vAlignment) {
            case 'top':
                this.alignment = TOP_ALIGNMENT[alignment];
                break;
            case 'middle':
                this.alignment = MIDDLE_ALIGNMENT[alignment];
                break;
            case 'bottom':
                this.alignment = BOTTOM_ALIGNMENT[alignment];
                break;
            default:
                this.alignment = TOP_ALIGNMENT.left;
        }
    }

    /**
     * 颜色归一化
     * @param r : number
     * @param g : number
     * @param b : number
     * @param a : number
     * @return {*[]}
     */
    static rgbToOne([r, g, b, a]) {
        let result = [r / 255, g / 255, b / 255, a / 255];
        PsdNode.fixFloatList(result);
        return result;
    }

    /**
     * 颜色数组转16位字符串表示
     * @param r : number
     * @param g : number
     * @param b : number
     * @param a : number
     * @return {string}
     */
    static rgbToHex([r, g, b, a]) {
        const bin = (r << 16 | g << 8 | b).toString(16);
        let rgb = `#${bin.padStart(6, '0')}`;
        let aBin = a.toString(16);
        return `${rgb}${aBin.padStart(2, '0')}`;
    }

    getOutputInfo() {
        return {
            name: this.name,
            type: this.type,
            content: this.content,
            pos: this.pos,
            size: this.size,
            font: this.font,
            fontSize: this.fontSize,
            color: this.color,
            italic: this.italic,
            bold: this.bold,
            underline: this.underline,
            alignment: this.alignment,
            lineSpace: this.lineSpace,
            letterSpacing: this.letterSpacing,
            colorRuns: this.colorRuns,
            runLengths: this.runLengths
        }
    }
}

module.exports = {
    PsdNode,
    PsdRootNode,
    PsdGroup,
    PsdImage,
    PsdText,
    PsdTemplate
};