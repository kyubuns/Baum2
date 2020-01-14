baum2
=====

## Photoshop(psd) to Unity(uGUI) Library!

- Photoshop
<img src="https://user-images.githubusercontent.com/961165/50334464-b9d5e680-054b-11e9-90ce-bfe14518d079.png" width="480">

- Unity
<img src="https://user-images.githubusercontent.com/961165/50334465-bb071380-054b-11e9-8c13-e7ce1fbd8a29.png" width="480">

## Setup ([Video](https://youtu.be/ugfyO0wRics))

### Photoshop

* Download [Baum.js](https://github.com/kyubuns/Baum2/releases)
* Copy to Photoshop/Plugins directory Baum.js
    - Mac OS: Applications\Adobe Photoshop [Photoshop_version]\Presets\Scripts
    - Windows 32 bit: Program Files (x86)\Adobe\Adobe Photoshop [Photoshop_version]\Presets\Scripts
    - Windows 64 bit: Program Files\Adobe\Adobe Photoshop [Photoshop_version](64 Bit)\Presets\Scripts

### Unity

* Download & Import [baum2.unitypackage](https://github.com/kyubuns/Baum2/blob/master/Baum2.unitypackage?raw=true)
* psd上で使用するFontは、BaumFontsファイルが置いてあるディレクトリに置いておいてください。
* (Please import the font used on psd in the directory where "BaumFonts" file is located.)

## How to use ([Video](https://youtu.be/2pIuC4MWT84))

### Photoshop上での操作

* psdを作ります。(psdの作り方参照)
* File -> Scripts -> Baum2を選択し、中間ファイルの出力先を選択します。

### Unity上での操作

* 生成された中間ファイルをBaum2/Importディレクトリ以下に投げ込みます。
* 自動的に「BaumPrefabs」を配置したディレクトリにprefabが出来上がります。
* 後は、Sample/Sample.csを参考にスクリプトからBaumUI.Instantiateで実行時に生成してください。

### psdの更新方法

* 同じように中間ファイルを生成後、Baum2/Importディレクトリ以下に投げ込むと、prefabが上書き更新されます。
    * この時、prefabのGUIDは変更されないためScriptに対する参照を張り直す必要はありません。

## psdの作り方

### 基本

基本的にPhotoshop上の1レイヤー = Unity上の1GameObjectになります。  
UIの一部をアニメーションさせたい場合などは、Photoshop上のレイヤーを分けておいてください。  

### Text

* Photoshop上の **Textレイヤー** は、Unity上でUnityEngine.UI.Textとして変換されます。
* フォントやフォントサイズ、色などの情報も可能な限りUnity側も同じように設定されます。

### Button

* Photoshop上の **名前が"Button"で終わるグループ** は、Unity上でUnityEngine.UI.Buttonとして変換されます。
* このグループ内で、最も奥に描画されるイメージレイヤーがクリック可能な範囲(UI.Button.TargetGraphic)に設定されます。

### Slider

* Photoshop上の **名前が"Slider"で終わるグループ** は、Unity上でUnityEngine.UI.Sliderとして変換されます。
* このグループ内で、名前がFillになっているイメージレイヤーがスライドするイメージ(UI.Slider.FillRect)になります。

### Scrollbar

* Photoshop上の **名前が"Scrollbar"で終わるグループ** は、Unity上でUnityEngine.UI.Scrollbarとして変換されます。
* このグループ内で、名前がHandleになっているイメージレイヤーがスライドするハンドル(UI.Scrollbar.HandleRect)になります。

### List

* Photoshop上の **名前が"List"で終わるグループ** は、Unity上でBaum2.Listとして変換されます。
* このグループ内には、Itemグループと、Maskレイヤーが必須です。
    * Itemグループ内の要素がリストの1アイテムになります。
    * Maskレイヤーがそのリストにかかるマスクになります。
* 詳しくはサンプルをご覧ください。

### Pivot

* Photoshop上のルート直下にあるグループにのみ使えます。
* 名前の後ろに *@Pivot=TopRight* のようにPivotを指定できます。

### コメントレイヤー

レイヤー名の先頭に#をつけることで、出力されないレイヤーを作ることが出来ます。

### 1334px以上を書き出す場合

- Baum.jsのmaxLengthを適切な値に変更して使ってください。
- デフォルトで1334に縮小している理由は、テクスチャのサイズを小さく抑えるためです。

## Developed by

* Unity: Unity2017, Unity2018, Unity2019
* PhotoshopScript: Adobe Photoshop CC 2018, Adobe Photoshop CC 2019
