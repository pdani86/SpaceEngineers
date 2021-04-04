#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QGraphicsScene>

#include "spaceeng.h"

QT_BEGIN_NAMESPACE
namespace Ui { class MainWindow; }
QT_END_NAMESPACE

class MainWindow : public QMainWindow
{
    Q_OBJECT

public:
    MainWindow(QWidget *parent = nullptr);
    ~MainWindow();

    void load();
    void loadImage();
    void updateScene();
    void saveToXML();

    void halveData();

private slots:
    void on_verticalSlider_valueChanged(int value);

    void on_saveBtn_clicked();

private:
    Ui::MainWindow *ui;

    QGraphicsScene scene;
    std::vector<uint8_t> _data;
    std::array<uint, 3> _dims{256,256,128};
    //std::vector<std::vector<uint8_t>> _data;
    int _curPlane = 0;
};
#endif // MAINWINDOW_H
