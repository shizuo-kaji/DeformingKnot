結び目描画ツール
====
結び目を自由に描画、移動、変形させることのできるツール。

## Oculus Questのコントローラーのボタンとpcのキーの対応

| コントローラー | pc |
| ---- | ---- |
| 右人差し指のトリガー | R |
| 右中指のトリガー | E |
| 左人差し指のトリガー | Q |
| 左中指のトリガー | W |
| Aボタン | A |
| Bボタン | B |
| Xボタン | X |
| Yボタン | Y |

## 使い方
`Base`、`ContiDeform`の2つの状態がある。左人差し指のトリガーで状態を切り替えることができる。

### Base
* 右人差し指のトリガー : 右手の動きに合わせて曲線を描画する。
* 右中指のトリガー : 右手に十分近い曲線を右手の動きに合わせて移動・回転させる。
<!-- * 左人差し指のトリガー : 選択されている曲線を Bezier 曲線で整形する。整形し終えると選択が解除される。 -->
* Aボタン : 曲線を選択・選択解除する。選択された曲線は黄色になる。
* Bボタン : 選択された曲線を手に十分近いところで切断する。
* Xボタン : 選択されている曲線が1つのときは曲線を閉曲線にする。2つのときは結合して1つの曲線にする。
* Yボタン : 選択されている曲線を削除する。

### ContiDeform

### pc上の場合のコントローラー操作
pc上で動作させる場合は白いキューブがコントローラーを表している。
コントローラーとカメラが同じ位置にあるのでPlay開始時にはコントローラーは見えない。
z軸正方向にいくらか動かすと視界に入るようになる。
| pcのキー | 移動方向 |
| ---- | ---- |
| ; | x軸正方向 |
| k | x軸負方向 |
| o | y軸正方向 |
| l | y軸負方向 |
| i | z軸正方向 |
| , | z軸負方向 |
