
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
    }
    init(dotNetImageSurvey) {
        this.dotNetObject = dotNetImageSurvey;
    }
    drawMarker(ctx, position, radius, color = "magenta", text = "") {
        let r = radius;
        let rh = r / 2;
        let x = position.x;
        let y = position.y;
        ctx.beginPath();
        ctx.arc(x, y, r, 0, 2 * Math.PI);
        ctx.moveTo(x + r, y);
        ctx.lineTo(x + rh, y);
        ctx.moveTo(x - r, y);
        ctx.lineTo(x - rh, y);
        ctx.moveTo(x, y + r);
        ctx.lineTo(x, y + rh);
        ctx.moveTo(x, y - r);
        ctx.lineTo(x, y - rh);
        ctx.strokeStyle = color;
        ctx.lineWidth = 4 / this.Zoom;
        ctx.stroke();
        ctx.font = "bold " + r.toFixed(0) + "px Arial";
        ctx.fillStyle = color;
        ctx.fillText(text, x + r * 1.20, y + rh);
    }
    getMarkerWidth() {
        return 60 / this.Zoom;
    }
    PrepareDisplay(divMain) {
        this.DivMain = divMain;
        this.DivMain.CodeBehind = this;
        //
        window.onresize = (ev) => { this.invalidate(); };
        this.Canvas = document.createElement('canvas');
        this.Canvas.addEventListener('pointerdown', this.canvasPointerActed);
        this.Canvas.addEventListener('pointermove', this.canvasPointerActed);
        this.DivMain.appendChild(this.Canvas);
        this.DivMain.addEventListener('resize', (event) => { this.invalidate(); });
    }
    cropAndSave(urlSaveNormedImage) {
    }
    draw() {
        if (!this.Image || this.Image.width < 1 || this.Image.height < 1 || !this.DivMain || this.DivMain.width < 1 || this.DivMain.height < 1) {
            return;
        }
        let rDivMain = this.DivMain.getBoundingClientRect();
        this.Canvas.height = rDivMain.height;
        this.Canvas.width = (this.Canvas.height * this.Image.width) / this.Image.height;
        let rCanvas = this.Canvas.getBoundingClientRect();
        this.Zoom = rCanvas.width / this.Image.width;
        const MW = PrepPic.getMarkerWidth();
        const MH = MW / 2;
        let ctx = this.Canvas.getContext("2d");
        ctx.resetTransform();
        ctx.scale(this.Zoom, this.Zoom);
        ctx.drawImage(this.Image, 0, 0);
        let ptHead = PrepPic.MeasureData.measurePoints[0];
        let ptBack = PrepPic.MeasureData.measurePoints[1];
        let ptsOnCircle = PrepPic.MeasureData.normalizePoints;
        PrepPic.drawMarker(ctx, ptHead, MH, 'magenta', 'Kopfspitze');
        PrepPic.drawMarker(ctx, ptBack, MH, 'brown', 'Kloake');
        if (this.Raw) {
            // Draw circle around Petri dish.
            for (let i = 0; i < ptsOnCircle.length; i++) {
                PrepPic.drawMarker(ctx, ptsOnCircle[i], MH, 'lightcyan', '');
            }
            let circle = PrepPic_t.GetCircleFrom3Points(
                ptsOnCircle[0].x,
                ptsOnCircle[0].y,
                ptsOnCircle[1].x,
                ptsOnCircle[1].y,
                ptsOnCircle[2].x,
                ptsOnCircle[2].y);
            ctx.beginPath();
            ctx.arc(circle.xCenter, circle.yCenter, circle.radius, 0, 2 * Math.PI);
            ctx.lineWidth = 4 / this.Zoom;
            ctx.stroke();
            //
            let fScale = circle.radius / 500;
            let fBoxSide = fScale * 600;
            let fLength = PrepPic_t.calcDistance(ptHead, ptBack);
            let ptBoxCenter = {
                x: (ptHead.x + ptBack.x) / 2,
                y: (ptHead.y + ptBack.y) / 2,
            };
            let phi = Math.atan2(ptHead.y - ptBack.y, ptHead.x - ptBack.x);
            ctx.translate(ptBoxCenter.x, ptBoxCenter.y);
            ctx.rotate(phi);
            ctx.strokeRect(-fBoxSide / 2, -fBoxSide / 2, fBoxSide, fBoxSide);
            //
            //glMatrix.mat2d.identity(this.M2D);
            //glMatrix.mat2d.translate(this.M2D, this.M2D, [300, 300]);
            //glMatrix.mat2d.scale(this.M2D, this.M2D, [1 / fScale, 1 / fScale]);
            //glMatrix.mat2d.rotate(this.M2D, this.M2D, -Math.PI / 2 - phi);
            //glMatrix.mat2d.translate(this.M2D, this.M2D, [-ptBoxCenter.x, -ptBoxCenter.y]);
            //ctxClipped.setTransform(this.M2D[0], this.M2D[1], this.M2D[2], this.M2D[3], this.M2D[4], this.M2D[5]);
            //ctxClipped.drawImage(this.Image, 0, 0);
        }
        this.CanvasValid = true;
    }
    invalidate() {
        this.CanvasValid = false;
        this.draw();
        this.refreshMeasurement();
    }
    canvasPointerActed(event) {
        const MW = PrepPic.getMarkerWidth();
        const MH = MW / 2;
        // console.log("Event-Typ: "+event.type+", button: "+event.button+", buttons: "+event.buttons);
        if (event.buttons == 1) {
            let rCanvas = PrepPic.Canvas.getBoundingClientRect();
            let p = { x: (event.clientX - rCanvas.left) / PrepPic.Zoom, y: (event.clientY - rCanvas.top) / PrepPic.Zoom };
            if (!PrepPic.DragPos) {
                PrepPic.DraggedPos = null;
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
                if (p != PrepPic.DragPos && PrepPic.DraggedPos) {
                    PrepPic.DraggedPos.x += (p.x - PrepPic.DragPos.x);
                    PrepPic.DraggedPos.y += (p.y - PrepPic.DragPos.y);
                    PrepPic.invalidate();
                    PrepPic.AnythingChanged = true;
                }
            }
            PrepPic.DragPos = p;
            PrepPic.dotNetObject.invokeMethodAsync('MeasureData_Changed', PrepPic.MeasureData);
        } else {
            PrepPic.DragPos = null;
        }
    }
    static calcDistance(p1, p2) {
        let dx = p2.x - p1.x;
        let dy = p2.y - p1.y;
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
    refreshMeasurement() {
        if (this.Head && this.Back && this.Element) {
            this.Element.InitIndivData();
            let md = this.Element.ElementProp.IndivData.MeasureData;
            md.HeadPosition = PrepPic.Head;
            md.BackPosition = PrepPic.Back;
            let fLength = 0;
            if (this.Raw) {
                if (this.PtsOnCircle) {
                    let circle = PrepPic_t.GetCircleFrom3Points(
                        this.PtsOnCircle[0].x,
                        this.PtsOnCircle[0].y,
                        this.PtsOnCircle[1].x,
                        this.PtsOnCircle[1].y,
                        this.PtsOnCircle[2].x,
                        this.PtsOnCircle[2].y);
                    let fScale = circle.radius / 500;
                    fLength = 0.1 * PrepPic_t.calcDistance(PrepPic.Head, PrepPic.Back) / fScale;
                    md.OrigHeadPosition = PrepPic.Head;
                    md.OrigBackPosition = PrepPic.Back;
                }
            } else {
                fLength = 0.1 * PrepPic_t.calcDistance(PrepPic.Head, PrepPic.Back);
            }
            if (fLength < 60) {
                md.HeadBodyLength = fLength;
            }
            this.MeasureResult.innerHTML = 'Kopf-Rumpf-Länge: ' + fLength.toFixed(1) + ' mm';
        }
    }
    setImage(urlImage,measureData) {
        if (!this.Image) {
            this.Image = document.createElement('img');
        }
        this.MeasureData = measureData;
        this.Raw = true;
        //this.PtsOnCircle = [
        //    { x: measureData.normalizePoints[0].x, y: measureData.normalizePoints[0].y },
        //    { x: measureData.normalizePoints[1].x, y: measureData.normalizePoints[1].y },
        //    { x: measureData.normalizePoints[2].x, y: measureData.normalizePoints[2].y },
        //];
        //this.Head = { x: measureData.measurePoints[0].x, y: measureData.measurePoints[0].y };
        //this.Back = { x: measureData.measurePoints[1].x, y: measureData.measurePoints[1].y };
        let imgSrc = urlImage;
        if (imgSrc != this.Image.src) {
            this.Image.src = imgSrc;
            if (this.Image) {
                this.Image.addEventListener("load", (ev) => { this.invalidate(); });
            }
        } else {
            this.invalidate();
        }
    }
}
window.PrepPic = new PrepPic_t();
