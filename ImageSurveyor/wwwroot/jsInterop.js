
class PrepPic_t {
    constructor() {
        this.DivMain = null;
        this.Canvas = null;
        this.CanvasValid = false;
        this.MeasureResult = null;
        this.Image = null;
        this.Raw = false;
        this.Zoom = 1;
        this.Head = { x: 300, y: 100 };
        this.Back = { x: 300, y: 500 };
        this.PtsOnCircle = [{ x: 300, y: 300 }, { x: 400, y: 400 }, { x: 500, y: 300 }];
        this.DragPos = null;
        this.DraggedPos = null;
        this.M2D = glMatrix.mat2d.create();
        this.AnythingChanged = false;
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
        let data = {
            ElementName: PrepPic.Element.ElementName,
            ImageData: PrepPic.ClippedCanvas.toDataURL("image/jpeg", 0.95),
        };
        PrepPic.Element.ElementProp.FileName = data.ImageData;
        PrepPic.Element.ElementProp.MarkerInfo.category = 350;
        let vHead = glMatrix.vec2.create();
        glMatrix.vec2.transformMat2d(vHead, [PrepPic.Head.x, PrepPic.Head.y], PrepPic.M2D);
        let vBack = glMatrix.vec2.create();
        glMatrix.vec2.transformMat2d(vBack, [PrepPic.Back.x, PrepPic.Back.y], PrepPic.M2D);
        let md = PrepPic.Element.ElementProp.IndivData.MeasuredData;
        md.HeadPosition = { x: vHead[0], y: vHead[1] };
        md.BackPosition = { x: vBack[0], y: vBack[1] };
        md.PtsOnCircle = PrepPic.PtsOnCircle;
        uploadElement(PrepPic.Element);
        PicMapElementMgr.UpdateElementMarker(PrepPic.Element);
        PrepPic.Element.refreshAccessibility();
        PrepPic.setRaw(false, false);
        PrepPic.setElement(PrepPic.Element, false);
        CurrentUser.ApiCall('/api/saveclippedimage.php', {
            method: 'POST',
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(data)
        }, [
        ])
            .then((response) => {
                response.text().then(function (text) {
                    console.log(text);
                });
            });
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
        PrepPic.drawMarker(ctx, this.Head, MH, 'magenta', 'Kopfspitze');
        PrepPic.drawMarker(ctx, this.Back, MH, 'brown', 'Kloake');
        if (this.Raw) {
            // Draw circle around Petri dish.
            for (let i = 0; i < this.PtsOnCircle.length; i++) {
                PrepPic.drawMarker(ctx, this.PtsOnCircle[i], MH, 'lightcyan', '');
            }
            let circle = PrepPic_t.GetCircleFrom3Points(
                this.PtsOnCircle[0].x,
                this.PtsOnCircle[0].y,
                this.PtsOnCircle[1].x,
                this.PtsOnCircle[1].y,
                this.PtsOnCircle[2].x,
                this.PtsOnCircle[2].y);
            ctx.beginPath();
            ctx.arc(circle.xCenter, circle.yCenter, circle.radius, 0, 2 * Math.PI);
            ctx.lineWidth = 4 / this.Zoom;
            ctx.stroke();
            //
            let fScale = circle.radius / 500;
            let fBoxSide = fScale * 600;
            let fLength = PrepPic_t.calcDistance(PrepPic.Head, PrepPic.Back);
            let ptBoxCenter = {
                x: (PrepPic.Head.x + PrepPic.Back.x) / 2,
                y: (PrepPic.Head.y + PrepPic.Back.y) / 2,
            };
            let phi = Math.atan2(this.Head.y - this.Back.y, this.Head.x - this.Back.x);
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
                if (PrepPic.Head && PrepPic_t.calcDistance(p, PrepPic.Head) <= MH) {
                    PrepPic.DraggedPos = PrepPic.Head;
                } else if (PrepPic.Back && PrepPic_t.calcDistance(p, PrepPic.Back) <= MH) {
                    PrepPic.DraggedPos = PrepPic.Back;
                } else {
                    for (let i = 0; i < PrepPic.PtsOnCircle.length; i++) {
                        if (PrepPic.PtsOnCircle[i] && PrepPic_t.calcDistance(p, PrepPic.PtsOnCircle[i]) <= MH) {
                            PrepPic.DraggedPos = PrepPic.PtsOnCircle[i];
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
            let md = this.Element.ElementProp.IndivData.MeasuredData;
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
    setImage(urlImage,measuredData) {
        if (!this.Image) {
            this.Image = document.createElement('img');
        }
        this.Raw = true;
        this.PtsOnCircle = [
            { x: measuredData.normalizePoints[0].x, y: measuredData.normalizePoints[0].y },
            { x: measuredData.normalizePoints[1].x, y: measuredData.normalizePoints[1].y },
            { x: measuredData.normalizePoints[2].x, y: measuredData.normalizePoints[2].y },
        ];
        this.Head = { x: measuredData.measurePoints[0].x, y: measuredData.measurePoints[0].y };
        this.Back = { x: measuredData.measurePoints[1].x, y: measuredData.measurePoints[1].y };
        let imgSrc = urlImage;
        if (imgSrc != this.Image.src) {
            this.Image.src = imgSrc;
            if (this.Image) {
                this.Image.addEventListener("load", (ev) => { this.invalidate(); });
            }
        }
    }
    setElement(element, bRefreshMode) {
        element.InitIndivData();
        this.Element = element;
        this.ElementDescription.innerHTML = element.getDescriptiveHtml();
        this.PtsOnCircle = null;
        let md = this.Element.ElementProp.IndivData && this.Element.ElementProp.IndivData.MeasuredData;
        if (md) {
            this.Head = md.HeadPosition;
            this.Back = md.BackPosition;
            if (md.PtsOnCircle && md.PtsOnCircle.length >= 3) {
                this.PtsOnCircle = md.PtsOnCircle;
            }
        }
        if (bRefreshMode) {
            let imgSrc = this.Element.getFileUrl(false);
            let bOnlyRawExists = (imgSrc.endsWith(".orig.jpg"));
            this.CheckBoxRaw.checked = bOnlyRawExists;
            this.Raw = bOnlyRawExists;
        }
        this.refreshImage();
        this.refreshMeasurement();
        this.AnythingChanged = false;
    }
}
window.PrepPic = new PrepPic_t();
