#include "mainwindow.h"
#include "ui_mainwindow.h"

#include <fstream>
#include <memory>
#include <string>

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent)
    , ui(new Ui::MainWindow)
{
    ui->setupUi(this);
    ui->graphicsView->setScene(&scene);

    load();
    //loadImage();
    ui->graphicsView->scale(3.0, 3.0);
}

MainWindow::~MainWindow()
{
    delete ui;
}

void MainWindow::load()
{
    std::string fname = "Head_voxel.dat";
    std::ifstream file;
    file.open(fname, std::ios::binary | std::ios::in);
    if(!file.is_open()) {
        qDebug("Couldn't open file!");
        return;
    }
    file.seekg(0, std::ios::end);
    auto fsize = file.tellg();
    file.seekg(0, std::ios::beg);
    qDebug(QString("file size: %1").arg(fsize).toUtf8().data());
    _data.resize(fsize);
    file.read(reinterpret_cast<char*>(_data.data()), fsize);
    if(!file) return;
    int countNonNull = 0;
    for(int i=0;i<fsize;i++) {if(_data[i] != 0) ++countNonNull;}
    qDebug(QString("NonNull: %1").arg(countNonNull).toUtf8().data());
    qDebug(QString("NonNull: %1 %%").arg(countNonNull / (double)fsize * 100.0).toUtf8().data());

    // halveData();
    // halveData();
}

void MainWindow::loadImage() {
    QImage img;
    img.load("img.png");
    _dims = {(uint)img.width(), (uint)img.height(), 1};
    img.convertTo(QImage::Format_Grayscale8);
    auto byteSize = img.width() * img.height();
    _data.resize(byteSize);
    memcpy(_data.data(), img.bits(), byteSize);
}

void MainWindow::updateScene()
{
    if(_data.size() < _dims[0] * _dims[1] * _dims[2]) return;

    auto plane = ui->verticalSlider->value() / (double)ui->verticalSlider->maximum();
    int planeIx = 0.5 + (plane * (_dims[2] - 1.0));
    auto zStep = (_dims[0] * _dims[1]);
    QImage img(_data.data() + planeIx * zStep, _dims[0], _dims[1], QImage::Format_Grayscale8);

    constexpr bool doColor = true;
    if(doColor) {
        QImage imgColor(img.width(), img.height(), QImage::Format_RGB888);
        for(int y=0; y < img.height(); ++y) {
            for(int x=0; x < img.width(); ++x) {
                int ix = y * img.bytesPerLine() + x;
                auto rgb = SpaceEng::val_to_rgb(*(img.bits() + ix));
                auto* p = imgColor.bits() + y*imgColor.bytesPerLine() + 3 * x;
                p[0] = rgb[0];
                p[1] = rgb[1];
                p[2] = rgb[2];
            }
        }
        img = imgColor;

    }

    scene.clear();
    scene.addPixmap(QPixmap::fromImage(img));
}

void MainWindow::saveToXML()
{
    // SpaceEng::saveXML(_data.data(), {256, 256, 128});

    /*auto zStep = (_dims[0] * _dims[1]);
    SpaceEng::saveXML(_data.data() + zStep * 54, {256, 256, 20});*/

    SpaceEng::saveXML(_data.data(), {(int)_dims[0], (int)_dims[1], (int)_dims[2]});
}

void MainWindow::on_verticalSlider_valueChanged(int value)
{
    updateScene();
}

void MainWindow::on_saveBtn_clicked()
{
    saveToXML();
}

void MainWindow::halveData() {
    decltype(_dims) newDims = {_dims[0] / 2, _dims[1] / 2, _dims[2] / 2};
    auto zStep = _dims[0] * _dims[1];
    auto yStep = _dims[0];
    auto zStepNew = newDims[0] * newDims[1];
    auto yStepNew = newDims[0];
    std::vector<uint8_t> newData;
    newData.resize(newDims[0] * newDims[1] * newDims[2]);
    for(int z = 0; z < newDims[2]; ++z) {
        for(int y = 0; y < newDims[1]; ++y) {
            for(int x = 0; x < newDims[0]; ++x) {
                int ix = (zStepNew * z + yStepNew * y + x);
                int origIx0 = 2 * (zStep * z + yStep * y + x);
                int sum = 0;
                sum += _data[origIx0 + zStep * 0 + yStep * 0 + 0];
                sum += _data[origIx0 + zStep * 0 + yStep * 0 + 1];
                sum += _data[origIx0 + zStep * 0 + yStep * 1 + 0];
                sum += _data[origIx0 + zStep * 0 + yStep * 1 + 1];
                sum += _data[origIx0 + zStep * 1 + yStep * 0 + 0];
                sum += _data[origIx0 + zStep * 1 + yStep * 0 + 1];
                sum += _data[origIx0 + zStep * 1 + yStep * 1 + 0];
                sum += _data[origIx0 + zStep * 1 + yStep * 1 + 1];

                newData[ix] = sum / 8;
            }
        }
    }
    _data = std::move(newData);
    _dims = newDims;
}

