import * as fs from "fs";
import * as PNGWrapper from "pngjs";
import * as PNGBrowser from "pngjs/browser";

import * as Path from "path";
export class Boarder {
  public Left: number;
  public Bottom: number;
  public Right: number;
  public Top: number;
  public xStart: number;
  public xEnd: number;
  public yStart: number;
  public yEnd: number;
  public skipX: boolean;
  public skipY: boolean;
  public constructor(
    left: number,
    bottom: number,
    right: number,
    top: number,
    xStart?: number,
    xEnd?: number,
    yStart?: number,
    yEnd?: number,
    skipX?: boolean,
    skipY?: boolean
  ) {
    this.Left = left;
    this.Bottom = bottom;
    this.Right = right;
    this.Top = top;
    if (xStart != null) this.xStart = xStart;
    if (xEnd != null) this.xEnd = xEnd;
    if (yStart != null) this.yStart = yStart;
    if (yEnd != null) this.yEnd = yEnd;
    if (skipX != null) this.skipX = skipX;
  }
}
export class SlicedTexture {
  public Texture: Texture2D;
  public Boarder: Boarder;

  public constructor(texture: Texture2D, boarder: Boarder) {
    this.Texture = texture;
    this.Boarder = boarder;
  }
}
export class Texture2D {
  public fullpath: string;
  public name: string;
  public width: number;
  public height: number;
  public data: Uint8Array;
  public png: PNGWrapper.PNG;
  constructor(fullpath: string | number, height: number = null) {
    let width: number;
    if (height != null) {
      width = (fullpath as unknown) as number;
      this.width = width;
      this.height = height;
    } else {
      fullpath = fullpath as string;
      this.fullpath = fullpath;
      this.GetPixels();
    }
  }
  public SetPixels(data: Uint8Array) {
    this.png.data = Buffer.from(data);
    this.png.height = this.height;
    this.png.width = this.width;
    let buffer = PNGBrowser.PNG.sync.write(this.png);
    fs.writeFileSync(this.fullpath, buffer);
  }
  private GetPixels() {
    let png = Helper.GetPixels(this.fullpath);
    this.png = png;
    this.width = png.width;
    this.height = png.height;
    this.data = png.data;
    this.name = Path.basename(this.fullpath, Path.extname(this.fullpath));
  }
}
export class Helper {
  public static GetPixels(fullpath: string) {
    let pngdata = fs.readFileSync(fullpath);
    let png = PNGBrowser.PNG.sync.read(pngdata) as PNGWrapper.PNG;
    return png;
  }
}

export class TextureSlicer {
  private readonly width: number;
  private readonly height: number;
  private readonly name: string;
  private readonly fullpath: string;
  private readonly pixels: Uint32Array;
  private readonly texture: Texture2D;
  private static SafetyMargin: number = 2;
  private static Margin: number = 2;

  public static Slice(texture?: Texture2D, fullpath?: string): SlicedTexture {
    // SlicedTexture
    if (texture instanceof Texture2D === false) {
      texture = new Texture2D(fullpath);
    }
    let pixels = texture.data;
    let slicer = new TextureSlicer(texture, pixels);
    return slicer.Slice(pixels);
  }

  public static GetBoarder(texture?: Texture2D, fullpath?: string): Boarder {
    // SlicedTexture
    if (texture instanceof Texture2D === false) {
      texture = new Texture2D(fullpath);
    }
    let pixels = texture.data;
    let slicer = new TextureSlicer(texture, pixels);
    return slicer.GetBoarder();
  }

  constructor(refTexture: Texture2D, getPixels: Uint8Array) {
    this.texture = refTexture;
    this.width = refTexture.width;
    this.height = refTexture.height;
    this.name = refTexture.name;
    this.fullpath = refTexture.fullpath;
    let pixelnum = this.height * this.width;
    let pixels = new Uint32Array(pixelnum);
    for (let y = 0; y < this.height; y++) {
      for (let x = 0; x < this.width; x++) {
        let idx = (this.width * y + x) << 2;
        let red = refTexture.data[idx];
        let green = refTexture.data[idx + 1];
        let blue = refTexture.data[idx + 2];
        let alpha = refTexture.data[idx + 3];
        // console.log(red + "," + green + "," + blue + "," + alpha);
        if (alpha === 0) {
          pixels[idx / 4] = 0;
        } else {
          const a: number = 256 / 4;
          if (alpha > 255 * 0.95) {
            alpha = 255;
          }
          if (alpha < 255 * 0.05) {
            alpha = 0;
          }
          pixels[idx / 4] =
            alpha * a * 255 * 255 +
            red * a * 255 +
            green * a +
            Math.floor((blue * a) / 255);
        }
        /* console.log(
          red + "," + green + "," + blue + "," + alpha + "=>" + pixels[idx / 4]
        ); */
      }
    }
    this.pixels = pixels;
  }

  private ToHashCode(): number {
    return 0;
  }

  private static CalcLine(list: number[]): number[] {
    let start = 0;
    let end = 0;
    let tmpStart = 0;
    let tmpEnd = 0;
    let tmpHash = list[0];
    for (let i = 0; i < list.length; ++i) {
      if (tmpHash === list[i]) {
        tmpEnd = i;
      } else {
        if (end - start < tmpEnd - tmpStart) {
          start = tmpStart;
          end = tmpEnd;
        }
        tmpStart = i;
        tmpEnd = i;
        tmpHash = list[i];
      }
    }
    if (end - start < tmpEnd - tmpStart) {
      start = tmpStart;
      end = tmpEnd;
    }

    end -= TextureSlicer.SafetyMargin * 2 + TextureSlicer.Margin;
    if (end < start) {
      start = 0;
      end = 0;
    }
    return [start, end];
  }

  private CreateHashListX(aMax: number, bMax: number): number[] {
    let hashList = new Array<number>(aMax);
    for (let a = 0; a < aMax; ++a) {
      let line: number = 0;
      for (let b = 0; b < bMax; ++b) {
        line = Math.floor(
          line + Math.floor(this.pixels[b * this.width + a] * b)
        ); // .GetHashCode();
      }
      hashList[a] = line;
    }
    return hashList;
  }

  private CreateHashListY(aMax: number, bMax: number): number[] {
    let hashList = new Array<number>(aMax);
    for (let a = 0; a < aMax; ++a) {
      let line: number = 0;
      for (let b = 0; b < bMax; ++b) {
        line = Math.floor(
          line + Math.floor(this.pixels[a * this.width + b] * b)
        ); // .GetHashCode();
      }
      hashList[a] = line;
    }
    return hashList;
  }

  private GetBoarder(): Boarder {
    let xStart: number;
    let xEnd: number;
    {
      let hashList = this.CreateHashListX(this.width, this.height);
      [xStart, xEnd] = TextureSlicer.CalcLine(hashList);
    }

    let yStart: number;
    let yEnd: number;
    {
      let hashList = this.CreateHashListY(this.height, this.width);
      [yStart, yEnd] = TextureSlicer.CalcLine(hashList);
    }

    let skipX = false;
    if (xEnd - xStart < 2) {
      skipX = true;
      xStart = 0;
      xEnd = 0;
    }

    let skipY = false;
    if (yEnd - yStart < 2) {
      skipY = true;
      yStart = 0;
      yEnd = 0;
    }
    let left = xStart + TextureSlicer.SafetyMargin;
    let bottom = yStart + TextureSlicer.SafetyMargin;
    let right =
      this.width - xEnd - TextureSlicer.SafetyMargin - TextureSlicer.Margin;
    let top =
      this.height - yEnd - TextureSlicer.SafetyMargin - TextureSlicer.Margin;
    if (skipX) {
      left = 0;
      right = 0;
    }
    if (skipY) {
      top = 0;
      bottom = 0;
    }
    return new Boarder(
      left,
      bottom,
      right,
      top,
      xStart,
      xEnd,
      yStart,
      yEnd,
      skipX,
      skipY
    );
  }
  private Slice(originalPixels: Uint8Array): SlicedTexture {
    let newboarder = this.GetBoarder();
    let output = this.GenerateSlicedTexture(
      newboarder.xStart,
      newboarder.xEnd,
      newboarder.yStart,
      newboarder.yEnd,
      originalPixels
    );
    return new SlicedTexture(output, newboarder);
  }

  private GenerateSlicedTexture(
    xStart: number,
    xEnd: number,
    yStart: number,
    yEnd: number,
    originalPixels: Uint8Array
  ): Texture2D {
    let outputWidth = this.width - (xEnd - xStart);
    let outputHeight = this.height - (yEnd - yStart);
    let outputPixels = new Uint8Array(outputWidth * outputHeight * 4);
    let originalX: number = 0;
    for (let x: number = 0; x < outputWidth; ++x, ++originalX) {
      if (originalX === xStart) {
        originalX += xEnd - xStart;
      }
      let originalY: number = 0;
      for (let y: number = 0; y < outputHeight; ++y, ++originalY) {
        if (originalY === yStart) {
          originalY += yEnd - yStart;
        }
        if (this.name === "listsample_piyoscrollbar_handle") {
          // cc.log(this.name);
        }
        outputPixels[(y * outputWidth + x) * 4] =
          originalPixels[(originalY * this.width + originalX) * 4];
        outputPixels[(y * outputWidth + x) * 4 + 1] =
          originalPixels[(originalY * this.width + originalX) * 4 + 1];
        outputPixels[(y * outputWidth + x) * 4 + 2] =
          originalPixels[(originalY * this.width + originalX) * 4 + 2];
        outputPixels[(y * outputWidth + x) * 4 + 3] =
          originalPixels[(originalY * this.width + originalX) * 4 + 3];
      }
    }
    let output = new Texture2D(outputWidth, outputHeight);
    output.fullpath = this.fullpath;
    output.png = this.texture.png;
    output.name = this.texture.name;
    output.SetPixels(outputPixels);
    return output;
  }

  private Get(x: number, y: number): number {
    return this.pixels[y * this.width + x];
  }
}
