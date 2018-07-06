# BTScript

A text-based behaviour script language for Unity.

# Motivation

The project is created in frustration that current Unity behaviour tree plugins (e.g. Behaviour Designer) is overly complex and feels strange to work with. As a programmer, I prefer a text based rather than dragging long branches of nodes.

Main features:

1. Simple script language syntax
2. Extensible (can easily create custom nodes)
3. Workflow is flexible and simple

# Installation

Clone the repository and copy the `Assets/Plugins/BTScript` folder into your project's same directory.

# VSCode plugin

BTScript has VSCode plugin support. Search `btscript` in extension marketplace. Source is in `./VSCodeExtension` folder.

* [Extension Page](https://marketplace.visualstudio.com/items?itemName=WeAthFolD.btscript)

# Usage

* Create a `.bts.txt` file in your project
* Write the script
* Assets -> Create -> Behaviour Script to create a new `BehaviourScript` asset
* Choose the asset, specify the `.bts.txt` file, click compile
* In your GameObject, create an `Behaviour Tree` component
* Select the corresponding `BehaviourScript` asset.

# Donation

Would appreciate a cup of coffee if you liked my work!

* Alipay ![](https://raw.githubusercontent.com/WeAthFoLD/BTScript/master/Misc/alipay.jpg)
* [Paypal](https://www.paypal.me/weathfold)

# TODO

* Write longer and detailed introduction
* Documentation of language syntax
* Better blackboard variable support, language wise / inspector wise
