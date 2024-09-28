
class PrepPic_t {
  constructor() {
    this.dotNetObject = null;
    this.DivMain = null;
    this.Canvas = null;
    this.CanvasValid = false;
    this.MeasureData = {};
    this.MeasureResult = null;
    this.Image = null;
    this.PolyLines = null;
    this.Raw = false;
    this.Zoom = 1;
    this.DragPos = null;
    this.DraggedPos = null;
    this.M2D = glMatrix.mat2d.create();
    this.AnythingChanged = false;
    this.LoadCnt = 0;
    this.HeadColor = 'magenta';
    this.BackColor = 'brown';
    this.SpineColor = 'magenta';
    this.NewEdgeColor = '#FF9EF0';
    this.SpineCurvePoints = [];
    this.SpineNewEdgePoints = [];
  }
  init(dotNetImageSurvey) {
    PrepPic.dotNetObject = dotNetImageSurvey;
  }
  drawMarker(ctx, position, radius, rotationRad = 0.0, color = "lightcyan", text = null) {
    let r = radius;
    let rh = r / 2;
    let x = position.X;
    let y = position.Y;
    ctx.beginPath();
    ctx.arc(x, y, r, 0, 2 * Math.PI);
    if (rotationRad != null) {
      [0.0, Math.PI / 2, Math.PI, -Math.PI / 2].forEach(rot => {
        let a = rotationRad + rot;
        ctx.moveTo(x + r * Math.cos(a), y + r * Math.sin(a));
        ctx.lineTo(x + rh * Math.cos(a), y + rh * Math.sin(a));
      });
    }
    ctx.strokeStyle = color;
    ctx.lineWidth = 3 / PrepPic.Zoom;
    ctx.stroke();
    if (text) {
      ctx.font = "bold " + r.toFixed(0) + "px Arial";
      ctx.fillStyle = color;
      ctx.fillText(text, x + r * 1.20, y + rh);
    }
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
    PrepPic.Canvas.addEventListener('pointerup', PrepPic.canvasPointerActed);
    PrepPic.Canvas.addEventListener('contextmenu', function (e) {
      e.preventDefault();
      e.stopPropagation();
    });
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
      if (["HeadToCloakInPetriDish", "HeadToCloakIn50mmCuvette"].includes(PrepPic.MeasureData.normalizer.NormalizeMethod)) {
        // Draw spine.
        {
          let l = PrepPic.SpineCurvePoints.length;
          // Curve points.
          PrepPic.SpineCurvePoints.forEach((pt, idx) => {
            let label = "";
            let color = PrepPic.SpineColor;
            let radius = MH / 2;
            let phi = null;
            if (idx == 0) {
              label = 'Kopfspitze';
              color = PrepPic.HeadColor;
              let ptNext = PrepPic.SpineCurvePoints[idx + 1];
              phi = Math.PI / 2 + Math.atan2(pt.Y - ptNext.Y, pt.X - ptNext.X);
              radius = MH;
            } else if (idx == l - 1) {
              label = 'Kloake';
              color = PrepPic.BackColor;
              let ptPrev = PrepPic.SpineCurvePoints[idx - 1];
              phi = Math.PI / 2 + Math.atan2(ptPrev.Y - pt.Y, ptPrev.X - pt.X);
              radius = MH;
            } else {
              color = PrepPic.SpineColor;
            }
            PrepPic.drawMarker(ctx, pt, radius, phi, color, label);
          });
          // Spine.
          ctx.beginPath();
          let polyLine = PrepPic_t.GetPolyLineFromRoundedCurve(PrepPic.SpineCurvePoints);
          ctx.moveTo(polyLine[0].X, polyLine[0].Y);
          polyLine.forEach(pt => ctx.lineTo(pt.X, pt.Y));
          ctx.strokeStyle = PrepPic.SpineColor;
          ctx.lineWidth = 4 / PrepPic.Zoom;
          ctx.stroke();
          // Potential new edge points
          PrepPic.SpineNewEdgePoints = [];
          PrepPic.SpineCurvePoints.forEach((pt, idx) => {
            if (idx >= 1) {
              let ptPrev = PrepPic.SpineCurvePoints[idx - 1];
              let ptNext = PrepPic.SpineCurvePoints[idx];
              let pt = {
                X: (ptPrev.X + ptNext.X) / 2,
                Y: (ptPrev.Y + ptNext.Y) / 2,
              };
              PrepPic.drawMarker(ctx, pt, MH / 3, null, PrepPic.NewEdgeColor);
              PrepPic.SpineNewEdgePoints.push(pt);
            }
          });
        }
      }
      if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakInPetriDish") {
        // Draw circle around Petri dish.
        let ptsOnCircle = PrepPic.MeasureData.normalizePoints;
        let circle = PrepPic_t.GetCircleFrom3Points(
          ptsOnCircle[0].X,
          ptsOnCircle[0].Y,
          ptsOnCircle[1].X,
          ptsOnCircle[1].Y,
          ptsOnCircle[2].X,
          ptsOnCircle[2].Y);
        ptsOnCircle.forEach((pt, idx) => {
          PrepPic.drawMarker(ctx, pt, MH);
        });
        ctx.beginPath();
        ctx.arc(circle.xCenter, circle.yCenter, circle.radius, 0, 2 * Math.PI);
        ctx.lineWidth = 4 / PrepPic.Zoom;
        ctx.stroke();
        // Draw normalize box.
        {
          let fScale = 2 * PrepPic.MeasureData.normalizer.NormalizePixelSize * circle.radius / PrepPic.MeasureData.normalizer.NormalizeReference;
          let fBoxWidth = fScale * PrepPic.MeasureData.normalizer.NormalizedWidthPx;
          let fBoxHeight = fScale * PrepPic.MeasureData.normalizer.NormalizedHeightPx;
          let ptHead = PrepPic.SpineCurvePoints[0];
          let ptBack = PrepPic.SpineCurvePoints[PrepPic.SpineCurvePoints.length - 1];
          let ptBoxCenter = {
            X: (ptHead.X + ptBack.X) / 2,
            Y: (ptHead.Y + ptBack.Y) / 2,
          };
          let phi = Math.PI / 2 + Math.atan2(ptHead.Y - ptBack.Y, ptHead.X - ptBack.X);
          ctx.translate(ptBoxCenter.X, ptBoxCenter.Y);
          ctx.rotate(phi);
          ctx.strokeRect(-fBoxWidth / 2, -fBoxHeight / 2, fBoxWidth, fBoxHeight);
        }
      } else if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakIn50mmCuvette") {
        // Draw markers on cuvette edges.
        let ptsNormalize = PrepPic.MeasureData.normalizePoints;
        let distX = ptsNormalize[1].X - ptsNormalize[0].X;
        let distY = ptsNormalize[1].Y - ptsNormalize[0].Y;
        let rotationBox = Math.atan2(distY, distX);
        let dist = Math.sqrt(distX * distX + distY * distY);
        let deltaX = 0.5 * dist * Math.cos(rotationBox + Math.PI / 2);
        let deltaY = 0.5 * dist * Math.sin(rotationBox + Math.PI / 2);
        for (let i = 0; i < ptsNormalize.length; i++) {
          PrepPic.drawMarker(ctx, ptsNormalize[i], MH, rotationBox);
        }
        ctx.beginPath();
        ctx.moveTo(ptsNormalize[0].X, ptsNormalize[0].Y);
        ctx.lineTo(ptsNormalize[1].X, ptsNormalize[1].Y);
        ctx.lineWidth = 4 / PrepPic.Zoom;
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(ptsNormalize[0].X - deltaX, ptsNormalize[0].Y - deltaY);
        ctx.lineTo(ptsNormalize[0].X + deltaX, ptsNormalize[0].Y + deltaY);
        ctx.moveTo(ptsNormalize[1].X - deltaX, ptsNormalize[1].Y - deltaY);
        ctx.lineTo(ptsNormalize[1].X + deltaX, ptsNormalize[1].Y + deltaY);
        ctx.lineWidth = 2 / PrepPic.Zoom;
        ctx.stroke();
        // Draw normalize box.
        let fNormDistance = PrepPic_t.calcDistance(ptsNormalize[1], ptsNormalize[0]);
        let fScale = PrepPic.MeasureData.normalizer.NormalizePixelSize * fNormDistance / PrepPic.MeasureData.normalizer.NormalizeReference;
        let fBoxWidth = fScale * PrepPic.MeasureData.normalizer.NormalizedWidthPx;
        let fBoxHeight = fScale * PrepPic.MeasureData.normalizer.NormalizedHeightPx;
        let ptHead = PrepPic.SpineCurvePoints[0];
        let ptBack = PrepPic.SpineCurvePoints[PrepPic.SpineCurvePoints.length - 1];
        let phi = Math.PI / 2 + Math.atan2(ptHead.Y - ptBack.Y, ptHead.X - ptBack.X);
        let ptBoxCenter = {
          X: (ptHead.X + ptBack.X) / 2,
          Y: (ptHead.Y + ptBack.Y) / 2,
        };
        ctx.translate(ptBoxCenter.X, ptBoxCenter.Y);
        ctx.rotate(phi);
        ctx.lineWidth = 4 / PrepPic.Zoom;
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
      if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakInPetriDish" || PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakIn50mmCuvette") {
        let ptHead = PrepPic.MeasureData.measurePoints[2];
        let ptBack = PrepPic.MeasureData.measurePoints[3];
        let ptsSpine = [ptHead];
        if (PrepPic.MeasureData.measurePoints.length >= 6) {
          let pt = PrepPic.MeasureData.measurePoints[5];
          ptsSpine.push(pt);
        }
        if (PrepPic.MeasureData.measurePoints.length >= 8) {
          let pt = PrepPic.MeasureData.measurePoints[7];
          ptsSpine.push(pt);
        }
        if (PrepPic.MeasureData.measurePoints.length >= 10) {
          let pt = PrepPic.MeasureData.measurePoints[9];
          ptsSpine.push(pt);
        }
        ptsSpine.push(ptBack);
        ctx.beginPath();
        let polyLine = PrepPic_t.GetPolyLineFromRoundedCurve(ptsSpine);
        ctx.moveTo(polyLine[0].X, polyLine[0].Y);
        polyLine.forEach(pt => ctx.lineTo(pt.X, pt.Y));
        ctx.strokeStyle = PrepPic.SpineColor;
        ctx.lineWidth = 3 / PrepPic.Zoom;
        ctx.stroke();
      } else if (PrepPic.MeasureData.normalizer.NormalizeMethod == "CropRectangle") {
      }
    }
    // Draw additional polylines.
    if (PrepPic.PolyLines) {
      ctx.resetTransform();
      ctx.scale(PrepPic.Zoom, PrepPic.Zoom);
      PrepPic.PolyLines.forEach(polyLine => {
        if (polyLine && polyLine.length >= 2) {
          ctx.beginPath();
          ctx.moveTo(polyLine[0].X, polyLine[0].Y);
          for (let j = 1; j < polyLine.length; j++) {
            ctx.lineTo(polyLine[j].X, polyLine[j].Y);
          }
          ctx.strokeStyle = 'limegreen';
          ctx.lineWidth = 2;
          ctx.stroke();
        }
      });
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
    let rCanvas = PrepPic.Canvas.getBoundingClientRect();
    let p = { X: (event.clientX - rCanvas.left) / PrepPic.Zoom, Y: (event.clientY - rCanvas.top) / PrepPic.Zoom };
    if (event.buttons == 1) {
      if (!PrepPic.DragPos) {
        PrepPic.DraggedPos = null;
        if (PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakInPetriDish" || PrepPic.MeasureData.normalizer.NormalizeMethod == "HeadToCloakIn50mmCuvette") {
          if (PrepPic.Raw) {
            let bHandled = false;
            PrepPic.SpineCurvePoints.forEach((pt, idx) => {
              if (PrepPic_t.calcDistance(p, pt) <= MH) {
                PrepPic.DraggedPos = pt;
                bHandled = true;
              }
            });
            if (!bHandled) {
              for (let i = 0; i < PrepPic.MeasureData.normalizePoints.length; i++) {
                if (PrepPic.MeasureData.normalizePoints[i] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.normalizePoints[i]) <= MH) {
                  PrepPic.DraggedPos = PrepPic.MeasureData.normalizePoints[i];
                  bHandled = true;
                }
              }
            }
            if (!bHandled) {
              PrepPic.SpineNewEdgePoints.forEach((pt, idx) => {
                if (PrepPic_t.calcDistance(p, pt) <= MH / 4) {
                  PrepPic.DraggedPos = pt;
                  bHandled = true;
                }
              });
            }
          }
        } else if (PrepPic.MeasureData.normalizer.NormalizeMethod == "CropRectangle") {
          if (PrepPic.Raw) {
            for (let i = 0; i < PrepPic.MeasureData.normalizePoints.length; i++) {
              if (PrepPic.MeasureData.normalizePoints[i] && PrepPic_t.calcDistance(p, PrepPic.MeasureData.normalizePoints[i]) <= MH) {
                PrepPic.DraggedPos = PrepPic.MeasureData.normalizePoints[i];
              }
            }
          }
        }
      } else {
        if (p != PrepPic.DragPos && PrepPic.DraggedPos && PrepPic.Raw) {
          PrepPic.DraggedPos.X += (p.X - PrepPic.DragPos.X);
          PrepPic.DraggedPos.Y += (p.Y - PrepPic.DragPos.Y);
          PrepPic.DraggedPos.X = Math.max(0, Math.min(PrepPic.Image.width, PrepPic.DraggedPos.X));
          PrepPic.DraggedPos.Y = Math.max(0, Math.min(PrepPic.Image.height, PrepPic.DraggedPos.Y));
          let nNewEdgeIdxToDelete = null;
          PrepPic.SpineNewEdgePoints.forEach((pt, idx) => {
            if (pt === PrepPic.DraggedPos) {
              PrepPic.SpineCurvePoints.splice(idx + 1, 0, pt);
              nNewEdgeIdxToDelete = idx;
            }
          });
          if (nNewEdgeIdxToDelete) {
            PrepPic.SpineNewEdgePoints.splice(nNewEdgeIdxToDelete, 1);
          }
          PrepPic.invalidate();
          PrepPic.AnythingChanged = true;
        }
      }
      PrepPic.DragPos = p;
    } else if (event.buttons == 2) {
      let idxSpineCurvePointToDelete = null;
      PrepPic.SpineCurvePoints.forEach((pt, idx) => {
        if (PrepPic_t.calcDistance(p, pt) <= MH) {
          idxSpineCurvePointToDelete = idx;
        }
      });
      if (idxSpineCurvePointToDelete) {
        PrepPic.SpineCurvePoints.splice(idxSpineCurvePointToDelete, 1);
        event.cancelBubble = true;
        PrepPic.invalidate();
        PrepPic.AnythingChanged = true;
      }
    } else {
      if (PrepPic.DragPos) {
        PrepPic.DragPos = null;
      }
    }
    if (PrepPic.AnythingChanged) {
      PrepPic.AnythingChanged = false;
      PrepPic.measureDataFromSpineCurvePoints();
      PrepPic.dotNetObject.invokeMethodAsync('MeasureData_Changed', JSON.stringify(PrepPic.MeasureData));
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
  static GetPolyLineFromRoundedCurve(points, Radius = null, MaxDeviation = 3.0) {
    let vaPolyLine = [];
    //
    if (Radius == null) {
      let pt1 = points[0];
      let pt2 = points[points.length - 1];
      Radius = PrepPic_t.calcDistance(pt1, pt2) / 3;
    }
    vaPolyLine.push(points[0]);
    for (let iPoint = 0; iPoint < points.length; iPoint++) {
      let ptf0 = points[(iPoint == 0) ? (points.length - 1) : (iPoint - 1)];
      let ptf1 = points[iPoint];
      let ptf2 = points[(iPoint == points.length - 1) ? 0 : (iPoint + 1)];
      let r = Radius;
      //
      if (r == 0 || iPoint == 0 || iPoint == points.length - 1) {
        // Es ist ein scharfes Eck.
        if (iPoint != 0) {
          vaPolyLine.push(ptf1);
        }
      } else {
        // Es ist ein abgerundetes Eck.
        let fAlpha1 = Math.atan2(ptf1.Y - ptf0.Y, ptf1.X - ptf0.X);
        let fAlpha2 = Math.atan2(ptf2.Y - ptf1.Y, ptf2.X - ptf1.X);
        let f2Beta = fAlpha1 + Math.PI - fAlpha2;
        while (f2Beta > 2 * Math.PI) {
          f2Beta -= 2 * Math.PI;
        }
        while (f2Beta <= 0) {
          f2Beta += 2 * Math.PI;
        }
        if (f2Beta > 0) {
          let fBeta = 0.5 * f2Beta;
          let nSign = (f2Beta < Math.PI) ? 1 : -1;
          let fA = Math.abs(r / Math.tan(fBeta));
          var ptfT0 = { X: ptf1.X - (fA * Math.cos(fAlpha1)), Y: ptf1.Y - (fA * Math.sin(fAlpha1)) };
          var ptfT1 = { X: ptf1.X + (fA * Math.cos(fAlpha2)), Y: ptf1.Y + (fA * Math.sin(fAlpha2)) };
          let fC = nSign * Math.sqrt((fA * fA) + (r * r));
          let fGamma = fAlpha1 - fBeta;
          var ptfM = { X: ptf1.X - (fC * Math.cos(fGamma)), Y: ptf1.Y - (fC * Math.sin(fGamma)) };
          let fStartAngle = fAlpha1 - (nSign * 0.5 * Math.PI);
          let fSweepAngle = Math.PI - f2Beta;
          //
          if (iPoint != 0 && vaPolyLine.length == 0) {
            vaPolyLine.push(ptfT0);
          }
          {
            var angleStepRad = Math.acos(r / (r + MaxDeviation));
            let nVertices = 1 + Math.ceil(Math.abs(fSweepAngle) / angleStepRad);
            for (let i = 0; i <= nVertices; i++) {
              let phi = fStartAngle + ((fSweepAngle * i) / nVertices);
              var vB = { X: (ptfM.X + (r * Math.cos(phi))), Y: (ptfM.Y + (r * Math.sin(phi))) };
              vaPolyLine.push(vB);
            }
          }
        }
      }
    }
    return vaPolyLine;
  }
  spineCurvePointsFromMeasureData() {
    let mp = PrepPic.MeasureData.measurePoints;
    if (mp.length < 2) {
      PrepPic.SpineCurvePoints = [];
    } else {
      PrepPic.SpineCurvePoints = [{ X: mp[0].X, Y: mp[0].Y }];
      for (let i = 4; i < mp.length; i += 2) {
        PrepPic.SpineCurvePoints.push({ X: mp[i].X, Y: mp[i].Y });
      }
      PrepPic.SpineCurvePoints.push({ X: mp[1].X, Y: mp[1].Y });
    }
  }
  measureDataFromSpineCurvePoints() {
    let mp = PrepPic.MeasureData.measurePoints;
    mp[0] = PrepPic.SpineCurvePoints[0];
    mp[1] = PrepPic.SpineCurvePoints[PrepPic.SpineCurvePoints.length - 1];
    mp.splice(4, mp.length - 4);
    for (let i = 1; i < PrepPic.SpineCurvePoints.length - 1; i++) {
      mp.push(PrepPic.SpineCurvePoints[i]);
      mp.push(PrepPic.SpineCurvePoints[i]);
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
    PrepPic.spineCurvePointsFromMeasureData();
    PrepPic.dotNetObject.invokeMethodAsync('MeasureData_Changed', JSON.stringify(PrepPic.MeasureData));
    if (urlImage != PrepPic.Image.src) {
      PrepPic.LoadCnt = 0;
      PrepPic.Image.src = urlImage;
    } else {
      PrepPic.invalidate();
    }
  }
  setPolyLines(polyLines) {
    PrepPic.PolyLines = JSON.parse(polyLines);
    PrepPic.invalidate();
  }
}
window.PrepPic = new PrepPic_t();
