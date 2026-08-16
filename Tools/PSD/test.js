/**
 * @desc 主入口
 * @author shenhz
 */

const path = require('path');
const PSD = require('./node_modules/psd');
const PsdTree = require('./Data/PsdTree');

(function () {
    try {
        const args = process.argv.slice(2);

        let psdPath = "C:/Users/Administrator/Downloads/编队_阵容信息_武器_切图.psd";
        let jsonPath = "F:/int/client/Assets\\GameRes/UI/_TempPrefab\\编队_阵容信息_武器_切图\\编队_阵容信息_武器_切图.json";
        let ignoreHideLayer = true;

        let psdName = path.basename(psdPath, '.psd');

        let psd = PSD.fromFile(psdPath);
        psd.parse();
        let tree = psd.tree();
        let psdTree = new PsdTree.PsdTree();
        psdTree.create(tree, psdName, ignoreHideLayer);
        psdTree.save(jsonPath);
        console.log(true)
    } catch (e) {
        console.log(e);
    }
}());
