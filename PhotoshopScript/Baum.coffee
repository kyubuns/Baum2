`#include "lib/json2.min.js"`

class Baum
  @version = '0.6.1'
  @maxLength = 1334

  run: ->
    @saveFolder = null
    if app.documents.length == 0
      filePaths = File.openDialog("Select a file", "*", true)
      for filePath in filePaths
        app.activeDocument = app.open(File(filePath))
        @runOneFile(true)

    else
      @runOneFile(false)

    alert('complete!')


  runOneFile: (after_close) =>
    @saveFolder = Folder.selectDialog("保存先フォルダの選択") if @saveFolder == null
    return if @saveFolder == null

    @documentName = app.activeDocument.name[0..-5]

    copiedDoc = app.activeDocument.duplicate(app.activeDocument.name[..-5] + '.copy.psd')
    Util.deselectLayers()
    @removeUnvisibleLayers(copiedDoc)
    @rasterizeAll(copiedDoc)
    @unvisibleAll(copiedDoc)
    @layerBlendAll(copiedDoc, copiedDoc)
    @removeCommentoutLayers(copiedDoc) # blendの処理してから消す
    @resizePsd(copiedDoc)
    @selectDocumentArea(copiedDoc)
    @ungroupArtboard(copiedDoc)
    @clipping(copiedDoc, copiedDoc)
    copiedDoc.selection.deselect()
    @psdToJson(copiedDoc)
    @psdToImage(copiedDoc)
    copiedDoc.close(SaveOptions.DONOTSAVECHANGES)
    app.activeDocument.close(SaveOptions.DONOTSAVECHANGES) if after_close


  selectDocumentArea: (document) ->
    x1 = 0
    y1 = 0
    x2 = document.width.value
    y2 = document.height.value
    selReg = [[x1,y1],[x2,y1],[x2,y2],[x1,y2]]
    document.selection.select(selReg)


  clipping: (document, root) ->
    if document.selection.bounds[0].value == 0 && document.selection.bounds[1].value == 0 && document.selection.bounds[2].value == document.width.value && document.selection.bounds[3].value == document.height.value
      return
    document.selection.invert()
    @clearAll(document, root)
    document.selection.invert()
    x1 = document.selection.bounds[0]
    y1 = document.selection.bounds[1]
    x2 = document.selection.bounds[2]
    y2 = document.selection.bounds[3]
    document.resizeCanvas(x2,y2,AnchorPosition.TOPLEFT)
    w = x2 - x1
    h = y2 - y1
    activeDocument.resizeCanvas(w,h,AnchorPosition.BOTTOMRIGHT)


  clearAll: (document, root) ->
    for layer in root.layers
      if layer.typename == 'LayerSet'
        @clearAll(document, layer)
      else if layer.typename == 'ArtLayer'
        if layer.kind != LayerKind.TEXT
          document.activeLayer = layer
          document.selection.clear()
      else
        alert(layer)


  resizePsd: (doc) ->
    width = doc.width
    height = doc.height
    return if width < Baum.maxLength && height < Baum.maxLength
    tmp = 0

    if width > height
      tmp = width / Baum.maxLength
    else
      tmp = height / Baum.maxLength

    width = width / tmp
    height = height / tmp
    doc.resizeImage(width, height, doc.resolution, ResampleMethod.NEARESTNEIGHBOR)


  removeUnvisibleLayers: (root) ->
    removeLayers = []

    for layer in root.layers
      if layer.visible == false
        removeLayers.push(layer)
        continue

      if layer.bounds[0].value == 0 && layer.bounds[1].value == 0 && layer.bounds[2].value == 0 && layer.bounds[3].value == 0
        removeLayers.push(layer)
        continue

      if layer.typename == 'LayerSet'
        @removeUnvisibleLayers(layer)

    if removeLayers.length > 0
      for i in [removeLayers.length-1..0]
        removeLayers[i].remove()


  removeCommentoutLayers: (root) ->
    removeLayers = []

    for layer in root.layers
      if layer.name.startsWith('#')
        removeLayers.push(layer)
        continue

      if layer.typename == 'LayerSet'
        @removeCommentoutLayers(layer)

    if removeLayers.length > 0
      for i in [removeLayers.length-1..0]
        removeLayers[i].remove()


  rasterizeAll: (root) ->
    for layer in root.layers
      if layer.name.startsWith('*')
        layer.name = layer.name[1..-1].strip()
        if layer.typename == 'LayerSet'
          Util.mergeGroup(layer)
        else
          @rasterize(layer)
      else if layer.typename == 'LayerSet'
        @rasterizeAll(layer)
      else if layer.typename == 'ArtLayer'
        if layer.kind != LayerKind.TEXT
          @rasterize(layer)
      else
        alert(layer)

    t = 0
    while(t < root.layers.length)
      if root.layers[t].visible && root.layers[t].grouped
        root.layers[t].merge()
      else
        t += 1

  rasterize: (layer) ->
    tmp = app.activeDocument.activeLayer
    app.activeDocument.activeLayer = layer

    layer.allLocked = false

    # LayerStyle含めてラスタライズ
    if layer.blendMode != BlendMode.OVERLAY && layer.kind != LayerKind.HUESATURATION
      Util.rasterizeLayerStyle(layer)

    # 普通にラスタライズ
    layer.rasterize(RasterizeType.ENTIRELAYER)

    # LayerMask
    Util.rasterizeLayerMask(layer)

    app.activeDocument.activeLayer = tmp

  ungroupArtboard: (document) ->
    for layer in document.layers
      if layer.name.startsWith('Artboard') && layer.typename == 'LayerSet'
        @ungroup(layer)

  ungroup: (root) ->
    layers = for layer in root.layers
      layer
    for i in [0...layers.length]
      layers[i].moveBefore(root)
    root.remove()

  unvisibleAll: (root) ->
    for layer in root.layers
      if layer.typename == 'LayerSet'
        @unvisibleAll(layer)
      else
        layer.visible = false

  layerBlendAll: (document, root) ->
    if root.layers.length == 0
      return

    for i in [root.layers.length-1..0]
      layer = root.layers[i]
      if layer.typename == 'LayerSet'
        @layerBlendAll(document, layer)
      else
        layer.visible = true
        continue if layer.blendMode != BlendMode.OVERLAY && layer.kind != LayerKind.HUESATURATION
        document.activeLayer = layer
        try
          # LayerKind.HUESATURATIONは0pxなのでエラーになる
          Util.selectTransparency()
          document.selection.bounds
          document.selection.copy(true)
        catch
          layer.copy(true)
        document.paste()
        newLayer = document.activeLayer
        newLayer.name = layer.name
        document.activeLayer = layer
        Util.selectTransparency()
        document.selection.invert()
        document.activeLayer = newLayer
        try
          document.selection.bounds
          document.selection.cut()
        layer.remove()

  psdToJson: (targetDocument) ->
    toJson = new PsdToJson()
    json = toJson.run(targetDocument, @documentName)
    Util.saveText(@saveFolder + "/" + @documentName + ".layout.txt", json)

  psdToImage: (targetDocument) ->
    toImage = new PsdToImage()
    json = toImage.run(targetDocument, @saveFolder, @documentName)



class PsdToJson
  run: (document, documentName) ->
    layers = @allLayers(document, document)
    imageSize = [document.width.value, document.height.value]
    canvasSize = [document.width.value, document.height.value]
    canvasBase = [document.width.value/2, document.height.value/2]

    canvasLayer = @findLayer(document, '#Canvas')
    if canvasLayer
      bounds = canvasLayer.bounds
      canvasSize = [bounds[2].value - bounds[0].value, bounds[3].value - bounds[1].value]
      canvasBase = [(bounds[2].value + bounds[0].value)/2, (bounds[3].value + bounds[1].value)/2]

    json = JSON.stringify({
      info: {
        version: Baum.version
        canvas: {
          image: {
            w: imageSize[0]
            h: imageSize[1]
          }
          size: {
            w: canvasSize[0]
            h: canvasSize[1]
          }
          base: {
            x: canvasBase[0]
            y: canvasBase[1]
          }
        }
      }
      root: {
        type: 'Root'
        name: documentName
        elements: layers
      }
    })
    json

  findLayer: (root, name) ->
    for layer in root.layers
      return layer if layer.name == name
    null


  allLayers: (document, root) ->
    layers = []
    for layer in root.layers when layer.visible
      hash = null
      name = layer.name.split("@")[0]
      opt = @parseOption(layer.name.split("@")[1])
      if layer.typename == 'ArtLayer'
        hash = @layerToHash(document, name, opt, layer)
      else
        hash = @groupToHash(document, name, opt, layer)
      if hash
        hash['name'] = name
        layers.push(hash)
    layers


  parseOption: (text) ->
    return {} unless text
    opt = {}
    for optText in text.split(",")
      elements = optText.split("=")
      elements[1] = 'true' if elements.length == 1
      opt[elements[0].toLowerCase()] = elements[1].toLowerCase()
    return opt

  layerToHash: (document, name, opt, layer) ->
    document.activeLayer = layer
    hash = {}
    if layer.kind == LayerKind.TEXT
      text = layer.textItem
      textSize = parseFloat(@getTextSize())
      textType = 'paragraph'
      scale = Util.getTextYScale(text) / 0.9

      if text.kind != TextType.PARAGRAPHTEXT
        text.kind = TextType.PARAGRAPHTEXT
        textType = 'point'

        text.height = textSize * (2.0 / scale)
        textCenterOffset = text.size.value
        pos = [text.position[0].value, text.position[1].value]
        pos[1] = pos[1] - (textCenterOffset / (2.0 / scale))
        text.position = pos

      originalText = text.contents.replace(/\r\n/g, '__CRLF__').replace(/\r/g, '__CRLF__').replace(/\n/g, '__CRLF__').replace(/__CRLF__/g, '\r\n')
      text.contents = "Z"

      bounds = Util.getTextExtents(text)
      vx = bounds.x
      vy = bounds.y
      ww = bounds.width
      hh = bounds.height
      vh = bounds.height

      align = ''
      textColor = 0x000000
      try
        align = text.justification.toString()[14..-1].toLowerCase()
        textColor = text.color.rgb.hexValue
      catch e
        align = 'left'

      hash = {
        type: 'Text'
        text: originalText
        textType: textType
        font: text.font
        size: textSize
        color: textColor
        align: align
        x: Math.round(vx * 100.0)/100.0
        y: Math.round(vy * 100.0)/100.0
        w: Math.round(ww * 100.0)/100.0
        h: Math.round(hh * 100.0)/100.0
        vh: Math.round(vh * 100.0)/100.0
        opacity: Math.round(layer.opacity * 10.0)/10.0
      }
      if Util.hasStroke(document, layer)
        hash['strokeSize'] = Util.getStrokeSize(document, layer)
        hash['strokeColor'] = Util.getStrokeColor(document, layer).rgb.hexValue
    else if opt['mask']
      hash = {
        type: 'Mask'
        image: Util.layerToImageName(layer)
        x: layer.bounds[0].value
        y: layer.bounds[1].value
        w: layer.bounds[2].value - layer.bounds[0].value
        h: layer.bounds[3].value - layer.bounds[1].value
        opacity: Math.round(layer.opacity * 10.0)/10.0
      }
    else
      hash = {
        type: 'Image'
        image: Util.layerToImageName(layer)
        x: layer.bounds[0].value
        y: layer.bounds[1].value
        w: layer.bounds[2].value - layer.bounds[0].value
        h: layer.bounds[3].value - layer.bounds[1].value
        opacity: Math.round(layer.opacity * 10.0)/10.0
      }
      hash['prefab'] = opt['prefab'] if opt['prefab']
      hash['background'] = true if opt['background']
    hash['pivot'] = opt['pivot'] if opt['pivot']
    hash['stretchx'] = opt['stretchx'] if opt['stretchx']
    hash['stretchy'] = opt['stretchy'] if opt['stretchy']
    hash['stretchxy'] = opt['stretchxy'] if opt['stretchxy']
    hash

  angleFromMatrix: (yy, xy) ->
    toDegs = 180/Math.PI
    return Math.atan2(yy, xy) * toDegs - 90

  getActiveLayerTransform: ->
    ref = new ActionReference()
    ref.putEnumerated( charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt") )
    desc = executeActionGet(ref).getObjectValue(stringIDToTypeID('textKey'))
    if (desc.hasKey(stringIDToTypeID('transform')))
      desc = desc.getObjectValue(stringIDToTypeID('transform'))
      xx = desc.getDouble(stringIDToTypeID('xx'))
      xy = desc.getDouble(stringIDToTypeID('xy'))
      yy = desc.getDouble(stringIDToTypeID('yy'))
      yx = desc.getDouble(stringIDToTypeID('yx'))
      return {xx: xx, xy: xy, yy: yy, yx: yx}
    return {xx: 0, xy: 0, yy: 0, yx: 0}


  getTextSize: ->
    ref = new ActionReference()
    ref.putEnumerated( charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt") )
    desc = executeActionGet(ref).getObjectValue(stringIDToTypeID('textKey'))
    textSize =  desc.getList(stringIDToTypeID('textStyleRange')).getObjectValue(0).getObjectValue(stringIDToTypeID('textStyle')).getDouble (stringIDToTypeID('size'))
    if (desc.hasKey(stringIDToTypeID('transform')))
      mFactor = desc.getObjectValue(stringIDToTypeID('transform')).getUnitDoubleValue (stringIDToTypeID("yy") )
      textSize = (textSize* mFactor).toFixed(2)
    return textSize


  groupToHash: (document, name, opt, layer) ->
    hash = {}
    if name.endsWith('Button')
      hash = { type: 'Button' }
    else if name.endsWith('List')
      hash = { type: 'List' }
      hash['scroll'] = opt['scroll'] if opt['scroll']
    else if name.endsWith('Slider')
      hash = { type: 'Slider' }
      hash['scroll'] = opt['scroll'] if opt['scroll']
    else if name.endsWith('Scrollbar')
      hash = { type: 'Scrollbar' }
      hash['scroll'] = opt['scroll'] if opt['scroll']
    else if name.endsWith('Toggle')
      hash = { type: 'Toggle' }
    else
      hash = { type: 'Group' }
    hash['pivot'] = opt['pivot'] if opt['pivot']
    hash['stretchx'] = opt['stretchx'] if opt['stretchx']
    hash['stretchy'] = opt['stretchy'] if opt['stretchy']
    hash['stretchxy'] = opt['stretchxy'] if opt['stretchxy']
    hash['elements'] = @allLayers(document, layer)
    hash


class PsdToImage
  baseFolder = null
  fileNames = []

  run: (document, saveFolder, documentName) ->
    @baseFolder = Folder(saveFolder + "/" + documentName)
    if @baseFolder.exists
      removeFiles = @baseFolder.getFiles()
      for i in [0...removeFiles.length]
        if removeFiles[i].name.startsWith(documentName) && removeFiles[i].name.endsWith('.png')
          removeFiles[i].remove()
      @baseFolder.remove()
    @baseFolder.create()

    targets = @allLayers(document)
    snapShotId = Util.takeSnapshot(document)
    for target in targets
      target.visible = true
      @outputLayer(document, target)
      Util.revertToSnapshot(document, snapShotId)


  allLayers: (root) ->
    for layer in root.layers when layer.kind == LayerKind.TEXT
      layer.visible = false

    list = for layer in root.layers when layer.visible
      if layer.typename == 'ArtLayer'
        layer.visible = false
        layer
      else
        @allLayers(layer)

    Array.prototype.concat.apply([], list) # list.flatten()


  outputLayer: (doc, layer) ->
    if !layer.isBackgroundLayer
      layer.translate(-layer.bounds[0], -layer.bounds[1])
      doc.resizeCanvas(layer.bounds[2] - layer.bounds[0], layer.bounds[3] - layer.bounds[1], AnchorPosition.TOPLEFT)
      doc.trim(TrimType.TRANSPARENT)

    layer.opacity = 100.0
    fileName = Util.layerToImageName(layer)
    if fileName in fileNames
      alert("#{fileName}と同名のレイヤーが存在します。レイヤー名を変更してください。")
    fileNames.push(fileName)
    saveFile = new File("#{@baseFolder.fsName}/#{fileName}.png")
    options = new ExportOptionsSaveForWeb()
    options.format = SaveDocumentType.PNG
    options.PNG8 = false
    options.optimized = true
    options.interlaced = false
    doc.exportDocument(saveFile, ExportType.SAVEFORWEB, options)


class Util
  @saveText: (filePath, text) ->
    file = File(filePath)
    file.encoding = "UTF8"
    file.open("w", "TEXT")
    file.write(text)
    file.close()

  @layerToImageName: (layer) ->
    encodeURI(Util.layerToImageNameLoop(layer)).replace(/%/g, '')

  @layerToImageNameLoop: (layer) ->
    return "" if layer instanceof Document
    image = Util.layerToImageName(layer.parent)
    imageName = image
    if imageName != ""
      imageName = imageName + "_"
    imageName + layer.name.split("@")[0].replace('_', '').replace(' ', '-').toLowerCase()

  @getLastSnapshotID: (doc) ->
    hsObj = doc.historyStates
    hsLength = hsObj.length
    for i in [hsLength-1 .. -1]
      if hsObj[i].snapshot
        return i

  @takeSnapshot: (doc) ->
    desc153 = new ActionDescriptor()
    ref119 = new ActionReference()
    ref119.putClass(charIDToTypeID("SnpS"))
    desc153.putReference(charIDToTypeID("null"), ref119 )
    ref120 = new ActionReference()
    ref120.putProperty(charIDToTypeID("HstS"), charIDToTypeID("CrnH") )
    desc153.putReference(charIDToTypeID("From"), ref120 )
    executeAction(charIDToTypeID("Mk  "), desc153, DialogModes.NO )
    return Util.getLastSnapshotID(doc)

  @revertToSnapshot: (doc, snapshotID) ->
    doc.activeHistoryState = doc.historyStates[snapshotID]

  @hasStroke: (doc, layer) ->
    doc.activeLayer = layer

    res = false
    ref = new ActionReference()
    ref.putEnumerated( charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt") )
    hasFX = executeActionGet(ref).hasKey(stringIDToTypeID('layerEffects'))
    if hasFX
      hasStroke = executeActionGet(ref).getObjectValue(stringIDToTypeID('layerEffects')).hasKey(stringIDToTypeID('frameFX'))
      if hasStroke
        desc1 = executeActionGet(ref)
        desc2 = executeActionGet(ref).getObjectValue(stringIDToTypeID('layerEffects')).getObjectValue(stringIDToTypeID('frameFX'))
        if desc1.getBoolean(stringIDToTypeID('layerFXVisible')) && desc2.getBoolean(stringIDToTypeID('enabled'))
          res = true
    return res

  @getStrokeSize: (doc, layer) ->
    doc.activeLayer = layer
    ref = new ActionReference()
    ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"))
    desc = executeActionGet(ref).getObjectValue(stringIDToTypeID('layerEffects')).getObjectValue(stringIDToTypeID('frameFX'))
    return desc.getUnitDoubleValue (stringIDToTypeID('size'))

  @getStrokeColor: (doc, layer) ->
    doc.activeLayer = layer
    ref = new ActionReference()
    ref.putEnumerated(charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt"))
    desc = executeActionGet(ref).getObjectValue(stringIDToTypeID('layerEffects')).getObjectValue(stringIDToTypeID('frameFX'))
    return Util.getColorFromDescriptor(desc.getObjectValue(stringIDToTypeID("color")), typeIDToCharID(desc.getClass(stringIDToTypeID("color"))))

  @getColorFromDescriptor: (colorDesc, keyClass) ->
    colorObject = new SolidColor()
    if keyClass == "Grsc"
      colorObject.grey.grey = color.getDouble(charIDToTypeID('Gry '))
    if keyClass == "RGBC"
      colorObject.rgb.red = colorDesc.getDouble(charIDToTypeID('Rd  '))
      colorObject.rgb.green = colorDesc.getDouble(charIDToTypeID('Grn '))
      colorObject.rgb.blue = colorDesc.getDouble(charIDToTypeID('Bl  '))
    if keyClass == "CMYC"
      colorObject.cmyk.cyan = colorDesc.getDouble(charIDToTypeID('Cyn '))
      colorObject.cmyk.magenta = colorDesc.getDouble(charIDToTypeID('Mgnt'))
      colorObject.cmyk.yellow = colorDesc.getDouble(charIDToTypeID('Ylw '))
      colorObject.cmyk.black = colorDesc.getDouble(charIDToTypeID('Blck'))
    if keyClass == "LbCl"
      colorObject.lab.l = colorDesc.getDouble(charIDToTypeID('Lmnc'))
      colorObject.lab.a = colorDesc.getDouble(charIDToTypeID('A   '))
      colorObject.lab.b = colorDesc.getDouble(charIDToTypeID('B   '))
    return colorObject

  @deselectLayers: ->
    desc01 = new ActionDescriptor()
    ref01 = new ActionReference()
    ref01.putEnumerated( charIDToTypeID('Lyr '), charIDToTypeID('Ordn'), charIDToTypeID('Trgt') )
    desc01.putReference( charIDToTypeID('null'), ref01 )
    executeAction( stringIDToTypeID('selectNoLayers'), desc01, DialogModes.NO )

  @selectTransparency: ->
    idChnl = charIDToTypeID( "Chnl" )
    actionSelect = new ActionReference()
    actionSelect.putProperty( idChnl, charIDToTypeID( "fsel" ) )
    actionTransparent = new ActionReference()
    actionTransparent.putEnumerated( idChnl, idChnl, charIDToTypeID( "Trsp" ) )
    actionDesc = new ActionDescriptor()
    actionDesc.putReference( charIDToTypeID( "null" ), actionSelect )
    actionDesc.putReference( charIDToTypeID( "T   " ), actionTransparent )
    executeAction( charIDToTypeID( "setd" ), actionDesc, DialogModes.NO )

  @getTextExtents: (text_item) ->
    app.activeDocument.activeLayer = text_item.parent
    ref = new ActionReference()
    ref.putEnumerated( charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt") )
    desc = executeActionGet(ref).getObjectValue(stringIDToTypeID('textKey'))
    bounds = desc.getObjectValue(stringIDToTypeID('bounds'))
    width = bounds.getUnitDoubleValue (stringIDToTypeID('right'))
    height = bounds.getUnitDoubleValue (stringIDToTypeID('bottom'))
    x_scale = 1
    y_scale = 1
    if desc.hasKey(stringIDToTypeID('transform'))
      transform = desc.getObjectValue(stringIDToTypeID('transform'))
      x_scale = transform.getUnitDoubleValue (stringIDToTypeID('xx'))
      y_scale = transform.getUnitDoubleValue (stringIDToTypeID('yy'))
    return { x:Math.round(text_item.position[0]), y:Math.round(text_item.position[1]) , width:Math.round(width*x_scale), height:Math.round(height*y_scale) }

  @getTextYScale: (text_item) ->
    app.activeDocument.activeLayer = text_item.parent
    ref = new ActionReference()
    ref.putEnumerated( charIDToTypeID("Lyr "), charIDToTypeID("Ordn"), charIDToTypeID("Trgt") )
    desc = executeActionGet(ref).getObjectValue(stringIDToTypeID('textKey'))
    bounds = desc.getObjectValue(stringIDToTypeID('bounds'))
    width = bounds.getUnitDoubleValue (stringIDToTypeID('right'))
    height = bounds.getUnitDoubleValue (stringIDToTypeID('bottom'))
    x_scale = 1
    y_scale = 1
    if desc.hasKey(stringIDToTypeID('transform'))
      transform = desc.getObjectValue(stringIDToTypeID('transform'))
      x_scale = transform.getUnitDoubleValue (stringIDToTypeID('xx'))
      y_scale = transform.getUnitDoubleValue (stringIDToTypeID('yy'))
    return y_scale

  @rasterizeLayerStyle: (layer) ->
    app.activeDocument.activeLayer = layer
    idrasterizeLayer = stringIDToTypeID("rasterizeLayer")
    desc5 = new ActionDescriptor()
    idnull = charIDToTypeID("null")
    ref4 = new ActionReference()
    idLyr = charIDToTypeID("Lyr ")
    idOrdn = charIDToTypeID("Ordn")
    idTrgt = charIDToTypeID("Trgt")
    ref4.putEnumerated(idLyr,idOrdn,idTrgt)
    desc5.putReference(idnull,ref4)
    idWhat = charIDToTypeID("What")
    idrasterizeItem = stringIDToTypeID("rasterizeItem")
    idlayerStyle = stringIDToTypeID("layerStyle")
    desc5.putEnumerated(idWhat,idrasterizeItem,idlayerStyle)
    executeAction(idrasterizeLayer,desc5,DialogModes.NO)

  @rasterizeLayerMask: (layer) ->
    app.activeDocument.activeLayer = layer
    if Util.hasVectorMask()
      Util.rasterizeLayer()
      Util.selectVectorMask()
      Util.rasterizeVectorMask()
      Util.applyLayerMask()

    if Util.hasLayerMask()
      Util.rasterizeLayer()
      Util.selectLayerMask()
      Util.applyLayerMask()

  @hasVectorMask: ->
    hasVectorMask = false
    try
      ref = new ActionReference()
      keyVectorMaskEnabled = app.stringIDToTypeID( 'vectorMask' )
      keyKind = app.charIDToTypeID( 'Knd ' )
      ref.putEnumerated( app.charIDToTypeID( 'Path' ), app.charIDToTypeID( 'Ordn' ), keyVectorMaskEnabled )
      desc = executeActionGet( ref )
      if desc.hasKey( keyKind )
        kindValue = desc.getEnumerationValue( keyKind )
        if (kindValue == keyVectorMaskEnabled)
          hasVectorMask = true
    catch e
      hasVectorMask = false
    return hasVectorMask

  @hasLayerMask: ->
    hasLayerMask = false
    try
      ref = new ActionReference()
      keyUserMaskEnabled = app.charIDToTypeID( 'UsrM' )
      ref.putProperty( app.charIDToTypeID( 'Prpr' ), keyUserMaskEnabled )
      ref.putEnumerated( app.charIDToTypeID( 'Lyr ' ), app.charIDToTypeID( 'Ordn' ), app.charIDToTypeID( 'Trgt' ) )
      desc = executeActionGet( ref )
      if desc.hasKey( keyUserMaskEnabled )
        hasLayerMask = true
    catch e
      hasLayerMask = false
    return hasLayerMask

  @rasterizeLayer: ->
    try
      id1242 = stringIDToTypeID( "rasterizeLayer" )
      desc245 = new ActionDescriptor()
      id1243 = charIDToTypeID( "null" )
      ref184 = new ActionReference()
      id1244 = charIDToTypeID( "Lyr " )
      id1245 = charIDToTypeID( "Ordn" )
      id1246 = charIDToTypeID( "Trgt" )
      ref184.putEnumerated( id1244, id1245, id1246 )
      desc245.putReference( id1243, ref184 )
      executeAction( id1242, desc245, DialogModes.NO )
    catch

  @selectVectorMask: ->
    try
      id55 = charIDToTypeID( "slct" )
      desc15 = new ActionDescriptor()
      id56 = charIDToTypeID( "null" )
      ref13 = new ActionReference()
      id57 = charIDToTypeID( "Path" )
      id58 = charIDToTypeID( "Path" )
      id59 = stringIDToTypeID( "vectorMask" )
      ref13.putEnumerated( id57, id58, id59 )
      id60 = charIDToTypeID( "Lyr " )
      id61 = charIDToTypeID( "Ordn" )
      id62 = charIDToTypeID( "Trgt" )
      ref13.putEnumerated( id60, id61, id62 )
      desc15.putReference( id56, ref13 )
      executeAction( id55, desc15, DialogModes.NO )
    catch e

  @selectLayerMask: ->
    try
      id759 = charIDToTypeID( "slct" )
      desc153 = new ActionDescriptor()
      id760 = charIDToTypeID( "null" )
      ref92 = new ActionReference()
      id761 = charIDToTypeID( "Chnl" )
      id762 = charIDToTypeID( "Chnl" )
      id763 = charIDToTypeID( "Msk " )
      ref92.putEnumerated( id761, id762, id763 )
      desc153.putReference( id760, ref92 )
      id764 = charIDToTypeID( "MkVs" )
      desc153.putBoolean( id764, false )
      executeAction( id759, desc153, DialogModes.NO )
    catch e

  @rasterizeVectorMask: ->
    try
      id488 = stringIDToTypeID( "rasterizeLayer" )
      desc44 = new ActionDescriptor()
      id489 = charIDToTypeID( "null" )
      ref29 = new ActionReference()
      id490 = charIDToTypeID( "Lyr " )
      id491 = charIDToTypeID( "Ordn" )
      id492 = charIDToTypeID( "Trgt" )
      ref29.putEnumerated( id490, id491, id492 )
      desc44.putReference( id489, ref29 )
      id493 = charIDToTypeID( "What" )
      id494 = stringIDToTypeID( "rasterizeItem" )
      id495 = stringIDToTypeID( "vectorMask" )
      desc44.putEnumerated( id493, id494, id495 )
      executeAction( id488, desc44, DialogModes.NO )
    catch e

  @applyLayerMask: ->
    try
      id765 = charIDToTypeID( "Dlt " )
      desc154 = new ActionDescriptor()
      id766 = charIDToTypeID( "null" )
      ref93 = new ActionReference()
      id767 = charIDToTypeID( "Chnl" )
      id768 = charIDToTypeID( "Ordn" )
      id769 = charIDToTypeID( "Trgt" )
      ref93.putEnumerated( id767, id768, id769 )
      desc154.putReference( id766, ref93 )
      id770 = charIDToTypeID( "Aply" )
      desc154.putBoolean( id770, true )
      executeAction( id765, desc154, DialogModes.NO )
    catch e

  @mergeGroup: (layer) ->
    app.activeDocument.activeLayer = layer
    try
      idMrgtwo = charIDToTypeID( "Mrg2" )
      desc15 = new ActionDescriptor()
      executeAction( idMrgtwo, desc15, DialogModes.NO )
    catch e


String.prototype.startsWith = (str) ->
  return this.slice(0, str.length) == str

String.prototype.endsWith = (suffix) ->
  return this.indexOf(suffix, this.length - suffix.length) != -1

String.prototype.strip = ->
  if String::trim? then @trim() else @replace /^\s+|\s+$/g, ""

setup = ->
  preferences.rulerUnits = Units.PIXELS
  preferences.typeUnits = TypeUnits.PIXELS

setup()
baum = new Baum()
baum.run()
