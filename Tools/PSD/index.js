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

        let psdPath = args[0];
        let jsonPath = args[1];
        let ignoreHideLayer = args[2] === 'ignoreHideLayer';
        // console.log(psdPath, jsonPath, ignoreHideLayer)

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
