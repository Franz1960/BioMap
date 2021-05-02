
class PrepPic_t {
  constructor() {
    this.dotNetObject = null;
    this.DivMain = null;
    this.Canvas = null;
    this.CanvasValid = false;
    this.MeasureData = {};
    this.MeasureResult = null;
    this.Image = null;
    this.Raw = false;
    this.Zoom = 1;
    this.DragPos = null;
    this.DraggedPos = null;
    this.M2D = glMatrix.mat2d.create();
    this.AnythingChanged = false;
    this.LoadCnt = 0;
  }
  init(dotNetImageSurvey) {
    PrepPic.dotNetObject = dotNetImageSurvey;
  }
  drawMarker(ctx, position, radius, rotationRad = 0.0, color = "lightcyan", text = "") {
    let r = radius;
    let rh = r / 2;
    let x = position.X;
    let y = position.Y;
    ctx.beginPath();
    ctx.arc(x, y, r, 0, 2 * Math.PI);
    [0.0, Math.PI / 2, Math.PI, -Math.PI / 2].forEach(rot => {
      let a = rotationRad + rot;
      ctx.moveTo(x + r * Math.cos(a), y + r * Math.sin(a));
      ctx.lineTo(x + rh * Math.cos(a), y + rh * Math.sin(a));
    });
    ctx.strokeStyle = color;
    ctx.lineWidth = 4 / PrepPic.Zoom;
    ctx.stroke();
    ctx.font = "bold " + r.toFixed(0) + "px Arial";
    ctx.fillStyle = color;
    ctx.fillText(text, x + r * 1.20, y + rh);
  }
  getMarkerWidth() {
    return 60 / PrepPic.Zoom;
  }
  PrepareDisplay(divMain) {
    PrepPic.DivMain = divMain;
    PrepPic.DivMain.CodeBehind = PrepPic;
    //
    window.onresize = (ev) => { PrepPic.invalidate(); };
    PrepPic.Canvas = document.createElement('canvas');
    PrepPic.Canvas.addEventListener('pointerdown', PrepPic.canvasPointerActed);
    PrepPic.Canvas.addEventListener('pointermove', PrepPic.canvasPointerActed);
    PrepPic.DivMain.appendChild(PrepPic.Canvas);
    PrepPic.DivMain.addEventListener('resize', (event) => { PrepPic.invalidate(); });
  }
  cropAndSave(urlSaveNormedImage) {
  }
  draw() {
    if (!PrepPic.Image || PrepPic.Image.width < 1 || PrepPic.Image.height < 1 || !PrepPic.DivMain || PrepPic.DivMain.width < 1 || PrepPic.DivMain.height < 1) {
      return;
    }
    let rDivMain = PrepPic.DivMain.getBoundingClientRect();
    PrepPic.Canvas.height = rDivMain.height;
    PrepPic.Canvas.width = (PrepPic.Canvas.height * PrepPic.Image.width) / PrepPic.Image.height;
    let rCanvas = PrepPic.Canvas.getBoundingClientRect();
    PrepPic.Zoom = rCanvas.width / PrepPic.Image.width;
    const MW = PrepPic.getMarkerWidth();
    const MH = MW / 2;
    let ctx = PrepPic.Canvas.getContext("2d");
    ctx.resetTransform();
    ctx.scale(PrepPic.Zoom, PrepPic.Zoom);
    ctx.drawImage(PrepPic.Image, 0, 0);
    if (PrepPic.Raw) {
      if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakInPetriDish") {
        let ptHead = PrepPic.MeasureData.measurePoints[0];
        let ptBack = PrepPic.MeasureData.measurePoints[1];
        let phi = Math.atan2(ptHead.Y - ptBack.Y, ptHead.X - ptBack.X);
        let ptsOnCircle = PrepPic.MeasureData.normalizePoints;
        PrepPic.drawMarker(ctx, ptHead, MH, phi, 'magenta', 'Kopfspitze');
        PrepPic.drawMarker(ctx, ptBack, MH, phi, 'brown', 'Kloake');
        // Draw circle around Petri dish.
        for (let i = 0; i < ptsOnCircle.length; i++) {
          PrepPic.drawMarker(ctx, ptsOnCircle[i], MH);
        }
        let circle = PrepPic_t.GetCircleFrom3Points(
          ptsOnCircle[0].X,
          ptsOnCircle[0].Y,
          ptsOnCircle[1].X,
          ptsOnCircle[1].Y,
          ptsOnCircle[2].X,
          ptsOnCircle[2].Y);
        ctx.beginPath();
        ctx.arc(circle.xCenter, circle.yCenter, circle.radius, 0, 2 * Math.PI);
        ctx.lineWidth = 4 / PrepPic.Zoom;
        ctx.stroke();
        //
        let fScale = circle.radius / 500;
        let fBoxSide = fScale * 600;
        let fLength = PrepPic_t.calcDistance(ptHead, ptBack);
        let ptBoxCenter = {
          X: (ptHead.X + ptBack.X) / 2,
          Y: (ptHead.Y + ptBack.Y) / 2,
        };
        ctx.translate(ptBoxCenter.X, ptBoxCenter.Y);
        ctx.rotate(phi);
        ctx.strokeRect(-fBoxSide / 2, -fBoxSide / 2, fBoxSide, fBoxSide);
      } else if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakIn50mmCuvette") {
        let ptHead = PrepPic.MeasureData.measurePoints[0];
        let ptBack = PrepPic.MeasureData.measurePoints[1];
        let phi = Math.PI / 2 + Math.atan2(ptHead.Y - ptBack.Y, ptHead.X - ptBack.X);
        let ptsNormalize = PrepPic.MeasureData.normalizePoints;
        PrepPic.drawMarker(ctx, ptHead, MH, phi, 'magenta', 'Kopfspitze');
        PrepPic.drawMarker(ctx, ptBack, MH, phi, 'brown', 'Kloake');
        // Draw circle around Petri dish.
        let rotationBox = Math.atan2(ptsNormalize[1].Y - ptsNormalize[0].Y, ptsNormalize[1].X - ptsNormalize[0].X);
        for (let i = 0; i < ptsNormalize.length; i++) {
          PrepPic.drawMarker(ctx, ptsNormalize[i], MH, rotationBox);
        }
        ctx.beginPath();
        ctx.moveTo(ptsNormalize[0].X, ptsNormalize[0].Y);
        ctx.lineTo(ptsNormalize[1].X, ptsNormalize[1].Y);
        ctx.lineWidth = 4 / PrepPic.Zoom;
        ctx.stroke();
        //
        let fNormDistance = PrepPic_t.calcDistance(ptsNormalize[1], ptsNormalize[0]);
        let fScale = fNormDistance / 500;
        let fBoxWidth = fScale * 500;
        let fBoxHeight = fScale * 1000;
        let fLength = PrepPic_t.calcDistance(ptHead, ptBack);
        let ptBoxCenter = {
          X: (ptHead.X + ptBack.X) / 2,
          Y: (ptHead.Y + ptBack.Y) / 2,
        };
        ctx.translate(ptBoxCenter.X, ptBoxCenter.Y);
        ctx.rotate(phi);
        ctx.strokeRect(-fBoxWidth / 2, -fBoxHeight / 2, fBoxWidth, fBoxHeight);
      } else if (PrepPic.MeasureData.normalizer.NormalizeMethod == "CropRectangle") {
        let ptA = PrepPic.MeasureData.normalizePoints[0];
        let ptB = PrepPic.MeasureData.normalizePoints[1];
        PrepPic.drawMarker(ctx, ptA, MH);
        PrepPic.drawMarker(ctx, ptB, MH);
        ctx.lineWidth = 4 / PrepPic.Zoom;
        ctx.strokeRect(ptA.X, ptA.Y, ptB.X - ptA.X, ptB.Y - ptA.Y);
      }
    } else {
      if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakInPetriDish") {
        let ptHead = PrepPic.MeasureData.measurePoints[2];
        let ptBack = PrepPic.MeasureData.measurePoints[3];
        PrepPic.drawMarker(ctx, ptHead, MH, 0, 'magenta', 'Kopfspitze');
        PrepPic.drawMarker(ctx, ptBack, MH, 0, 'brown', 'Kloake');
      } else if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakIn50mmCuvette") {
        let ptHead = PrepPic.MeasureData.measurePoints[2];
        let ptBack = PrepPic.MeasureData.measurePoints[3];
        PrepPic.drawMarker(ctx, ptHead, MH, 0, 'magenta', 'Kopfspitze');
        PrepPic.drawMarker(ctx, ptBack, MH, 0, 'brown', 'Kloake');
      } else if (PrepPic.MeasureData.normalizer.NormalizeMethod == "CropRectangle") {
      }
    }
    PrepPic.CanvasValid = true;
  }
  invalidate() {
    PrepPic.CanvasValid = false;
    PrepPic.draw();
  }
  canvasPointerActed(event) {
    const MW = PrepPic.getMarkerWidth();
    const MH = MW / 2;
    if (event.buttons == 1) {
      let rCanvas = PrepPic.Canvas.getBoundingClientRect();
      let p = { X: (event.clientX - rCanvas.left) / PrepPic.Zoom, Y: (event.clientY - rCanvas.top) / PrepPic.Zoom };
      if (!PrepPic.DragPos) {
        PrepPic.DraggedPos = null;
        if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakInPetriDish") {
          if (PrepPic.Raw) {
            if (PrepPic.MeasureData.measurePoints[0] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.measurePoints[0]) <= MH) {
              PrepPic.DraggedPos = PrepPic.MeasureData.measurePoints[0];
            } else if (PrepPic.MeasureData.measurePoints[1] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.measurePoints[1]) <= MH) {
              PrepPic.DraggedPos = PrepPic.MeasureData.measurePoints[1];
            } else {
              for (let i = 0; i < PrepPic.MeasureData.normalizePoints.length; i++) {
                if (PrepPic.MeasureData.normalizePoints[i] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.normalizePoints[i]) <= MH) {
                  PrepPic.DraggedPos = PrepPic.MeasureData.normalizePoints[i];
                }
              }
            }
          } else {
            if (PrepPic.MeasureData.measurePoints[2] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.measurePoints[2]) <= MH) {
              PrepPic.DraggedPos = PrepPic.MeasureData.measurePoints[2];
            } else if (PrepPic.MeasureData.measurePoints[3] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.measurePoints[3]) <= MH) {
              PrepPic.DraggedPos = PrepPic.MeasureData.measurePoints[3];
            }
          }
        } else if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakIn50mmCuvette") {
          if (PrepPic.Raw) {
            if (PrepPic.MeasureData.measurePoints[0] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.measurePoints[0]) <= MH) {
              PrepPic.DraggedPos = PrepPic.MeasureData.measurePoints[0];
            } else if (PrepPic.MeasureData.measurePoints[1] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.measurePoints[1]) <= MH) {
              PrepPic.DraggedPos = PrepPic.MeasureData.measurePoints[1];
            } else {
              for (let i = 0; i < PrepPic.MeasureData.normalizePoints.length; i++) {
                if (PrepPic.MeasureData.normalizePoints[i] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.normalizePoints[i]) <= MH) {
                  PrepPic.DraggedPos = PrepPic.MeasureData.normalizePoints[i];
                }
              }
            }
          } else {
            if (PrepPic.MeasureData.measurePoints[2] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.measurePoints[2]) <= MH) {
              PrepPic.DraggedPos = PrepPic.MeasureData.measurePoints[2];
            } else if (PrepPic.MeasureData.measurePoints[3] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.measurePoints[3]) <= MH) {
              PrepPic.DraggedPos = PrepPic.MeasureData.measurePoints[3];
            }
          }
        } else if (PrepPic.MeasureData.normalizer.NormalizeMethod == "CropRectangle") {
          if (PrepPic.Raw) {
            for (let i = 0; i < PrepPic.MeasureData.normalizePoints.length; i++) {
              if (PrepPic.MeasureData.normalizePoints[i] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.normalizePoints[i]) <= MH) {
                PrepPic.DraggedPos = PrepPic.MeasureData.normalizePoints[i];
              }
            }
          } else {
          }
        }
      } else {
        if (p != PrepPic.DragPos && PrepPic.DraggedPos) {
          PrepPic.DraggedPos.X += (p.X - PrepPic.DragPos.X);
          PrepPic.DraggedPos.Y += (p.Y - PrepPic.DragPos.Y);
          PrepPic.DraggedPos.X = Math.max(0, Math.min(PrepPic.Image.width, PrepPic.DraggedPos.X));
          PrepPic.DraggedPos.Y = Math.max(0, Math.min(PrepPic.Image.height, PrepPic.DraggedPos.Y));
          PrepPic.invalidate();
          PrepPic.AnythingChanged = true;
        }
      }
      PrepPic.DragPos = p;
      PrepPic.dotNetObject.invokeMethodAsync('MeasureData_Changed', JSON.stringify(PrepPic.MeasureData));
    } else {
      PrepPic.DragPos = null;
    }
  }
  static calcDistance(p1, p2) {
    let dx = p2.X - p1.X;
    let dy = p2.Y - p1.Y;
    return Math.sqrt(dx * dx + dy * dy);
  }
  static GetCircleFrom3Points(x1, y1, x2, y2, x3, y3) {
    // Sort points.
    if (Math.abs(y2 - y3) <= Math.abs(y1 - y2)) {
      let fTemp = y3;
      y3 = y1;
      y1 = fTemp;
      fTemp = x3;
      x3 = x1;
      x1 = fTemp;
    }
    let ms2 = (x3 - x2) / (y2 - y3);
    let f1;
    if (y1 == y2) {
      f1 = x1 + x2;
    } else {
      let ms1 = (x2 - x1) / (y1 - y2);
      let msD = ms1 - ms2;
      f1 = ((x1 + x2) * ms1 - (x2 + x3) * ms2 + (y3 - y1)) / msD;
    }
    let xCenter = f1 / 2;
    let yCenter = ((f1 - x2 - x3) * ms2 + (y2 + y3)) / 2;
    let radius = Math.sqrt((xCenter - x1) * (xCenter - x1) + (yCenter - y1) * (yCenter - y1));
    return {
      xCenter: xCenter,
      yCenter: yCenter,
      radius: radius,
    }
  }
  setImage(urlImage, bRaw, measureData) {
    if (!PrepPic.Image) {
      PrepPic.Image = document.createElement('img');
      PrepPic.Image.addEventListener("load", (ev) => {
        PrepPic.LoadCnt++;
        PrepPic.invalidate();
      });
    }
    PrepPic.Raw = bRaw;
    PrepPic.MeasureData = JSON.parse(measureData);
    PrepPic.dotNetObject.invokeMethodAsync('MeasureData_Changed', JSON.stringify(PrepPic.MeasureData));
    if (urlImage != PrepPic.Image.src) {
      PrepPic.LoadCnt = 0;
      PrepPic.Image.src = urlImage;
    } else {
      PrepPic.invalidate();
    }
  }
}
window.PrepPic = new PrepPic_t();
