/**
 * @desc psd树生成工厂
 * @author shenhz
 */

const PsdData = require('./PsdData');
const FileUtil = require('../Core/FileUtil');

class PsdTree {
    constructor() {
        /**
         * 树的根节点
         * @type {PsdRootNode}
         */
        this.tree = undefined;
        this.ignoreHideLayer = false;
    }

    /**
     * 创建树
     * @param treeData
     * @param psdName
     * @param ignoreHideLayer
     */
    create(treeData, psdName, ignoreHideLayer) {
        // 获取原始尺寸
        let resourceSize = [treeData.coords.right, treeData.coords.bottom];
        this.ignoreHideLayer = ignoreHideLayer;
        this.tree = new PsdData.PsdRootNode(psdName, resourceSize);
        this.createChildren(this.tree, treeData.children(), resourceSize);
        this.tree.childReverse();
        this.tree.prepare();
        this.tree.afterAction();
    }

    /**
     * 创建子节点
     * @private
     * @param parent : PsdNode
     * @param children : *[]
     * @param resourceSize : number[]
     */
    createChildren(parent, children, resourceSize) {
        children.forEach(child => {
            // 不显示的就不管了
            if (this.ignoreHideLayer && !child.layer.visible) {
                return;
            }

            let node;
            // 按照类型来解析
            switch (child.type) {
                case 'group':
                    node = new PsdData.PsdGroup(child, resourceSize, parent);
                    // 子节点递归解析
                    this.createChildren(node, child.children(), resourceSize);
                    break;
                case 'layer':
                    // 看看是图片、模板还是文本
                    let exportData = child.export();
                    if (exportData.text) {
                        // 文本
                        node = new PsdData.PsdText(child, resourceSize, parent);
                    } else {
                        // 图片或者是模板
                        // if (child.name.startsWith('Tmp_')) {
                        // 	// 模板
                        // 	node = new PsdData.PsdTemplate(child, resourceSize, parent);
                        // } else {
                        // 普通图片
                        node = new PsdData.PsdImage(child, resourceSize, parent);
                        // }
                    }
            }

            if (node) {
                parent.childNodes.push(node);
            }
        });
    }

    /**
     * 保存数据
     * @param path
     */
    save(path) {
        let info = this.tree.getOutputInfo()
        FileUtil.writeFileAsJson(info, path);
    }
}

module.exports = {
    PsdTree
};

