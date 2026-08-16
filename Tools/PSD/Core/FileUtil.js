/**
 * 文件操作相关辅助工具
 * 读取一个文件夹下面所有指定特征的文件内容
 * 读取一个指定文件的内容
 * 获取一个文件夹下面所有指定特征的文件名列表
 * @author shenhz
 */

const Fs = require('fs');

module.exports = {
    /**
     * 以json格式读取指定文件的内容
     * @param path
     * @returns {any}
     */
    readFileAsJson(path) {
        if (!Fs.existsSync(path)) {
            return undefined;
        }

        let data = Fs.readFileSync(path);
        if (data) {
            return JSON.parse(data);
        }
    },

    /**
     * 以json格式的内容写入（覆盖）指定文件
     * @param data
     * @param path
     */
    writeFileAsJson(data, path) {
        if (!data)
            return;

        if (Fs.existsSync(path)) {
            // 存在这份文件就删咯重写
            Fs.unlinkSync(path);
        }
        Fs.writeFileSync(path, JSON.stringify(data, null, 2));

    },

};