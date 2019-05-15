// panel/index.js
const Path = require("path");
// @ts-ignore
Editor.Panel.extend({
    // dependencies: [
    //     '../lib/psd.js',
    //   ],
    style: `
      :host { margin: 5px; }
      h2 { color: #f90; }     
      #image {
        text-align: center;
      }
    `,
    template: `
    <script type="text/javascript" src="../lib/psd.js">console.log('psd.js ok!');Editor.log('psd.js ok!');</script>
      <h2>レイアウトファイルのフルパス</h2><input type="file" id="layoutfileElement" value="" />      
      <ui-input style="width: 2000px;" id='layoutPath'  class="huge" placeholder="baum2で出力されたレイアウトファイルのフルパスを入力してください。例：C:\\testcocos\\Sample.psd.baum2\\Sample.layout.txt"></ui-input>
      <br>
      <br>
      <h2>PNGファイルのフォルダ</h2><input type="file" id="pngfileElement" value="" webkitdirectory />      
      <ui-input style="width: 2000px;" id='pngFolder'  class="huge" placeholder="baum2で出力されたPNGファイルのフォルダパスを入力してください。例：C:\\testcocos\\Sample.psd.baum2\\Sample"></ui-input>
      <br>
      <ui-button id="psd_convert_btn">PSからUIへconvert</ui-button>
      <br>
      <h2>注意:</h2>  
      <h3>・利用するフォントは、予め「<b style="color:black;" id="fontUrl">db://assets/resources/font/</b>」に置いてください。</h3> 
      <h3>・PNGファイル9slice化され、結果が「<b style="color:black;"  id="pngAssetUrl">db://assets/Texture/[PNGファイルのフォルダ名]</b>」に保存されます。</h3> 
      <h3>・生成されたprefabは「<b style="color:black;"  id="prefabUrl">db://assets/Prefab/[PNGファイルのフォルダ名].prefab</b>」に保存されます。</h3>     
  
    `,
    $: {
        layoutfileElement: "#layoutfileElement",
        pngfileElement: "#pngfileElement",
        layoutPath: "#layoutPath",
        pngFolder: "#pngFolder",
        psd_convert_btn: "#psd_convert_btn",
        fontUrl: "#fontUrl",
        pngAssetUrl: "#pngAssetUrl",
        prefabUrl: "#prefabUrl"
    },
    ready() {
        // @ts-ignore
        Editor.Ipc.sendToMain("psd-convert-to-node:print-text", "psd-convert-to-node ready");
        this.$layoutfileElement.onchange = ev => {
            let layoutfileElement = this.$layoutfileElement;
            this.$layoutPath.value = layoutfileElement.files[0].path;
            let pngAssetUrl = this.$pngAssetUrl;
            console.log(pngAssetUrl);
        };
        this.$pngfileElement.onchange = ev => {
            let pngfileElement = this.$pngfileElement;
            this.$pngFolder.value = pngfileElement.files[0].path;
            let pngAssetUrl = this.$pngAssetUrl;
            let prefabUrl = this.$prefabUrl;
            let basename = Path.basename(pngfileElement.files[0].path, Path.extname(pngfileElement.files[0].path));
            pngAssetUrl.innerText = "db://assets/Texture/" + basename + "/";
            prefabUrl.innerText = "db://assets/Prefab/" + basename + ".prefab";
            console.log(ev);
        };
        this.$psd_convert_btn.addEventListener("confirm", () => {
            let layoutPath = this.$layoutPath.value;
            let pngFolder = this.$pngFolder.value;
            // let nodeName = this.$mountNode.value;
            if (!layoutPath) {
                // @ts-ignore
                Editor.error("レイアウトファイルのフルパスを入力してください");
                return;
            }
            if (!pngFolder) {
                // @ts-ignore
                Editor.error("PNGファイルのフォルダのフルパスを入力してください");
                return;
            }
            // @ts-ignore
            Editor.log("value,layoutPath:", layoutPath);
            // @ts-ignore
            Editor.log("value,pngFolder:", pngFolder);
            // this.$label.innerText = "変換開始," + new Date().valueOf().toString();
            let isReverse = false;
            let isSaveJson = false;
            // @ts-ignore
            Editor.Ipc.sendToMain("psd-convert-to-node:convet-psd-file-by-fullpath", [isReverse, isSaveJson, layoutPath, pngFolder], msg => {
                // @ts-ignore
                Editor.log("psdからノードツリーに変換完了，msg:", msg, ",nodeName:", "Canvas");
            }, 1000 * 1000);
        });
    },
    messages: {
        "receive-msg"(ev, text) {
            // @ts-ignore
            Editor.log("PSD2Nodeパネル メッセージFromメインプロセス：", text);
        },
        "get-mount-node-name"(ev) {
            if (ev.reply) {
                ev.reply("Canvas");
            }
        }
    }
});
//# sourceMappingURL=index.js.map