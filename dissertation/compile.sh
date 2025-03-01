#!/bin/bash

# Clean previous compilation files
rm -f report/*.{aux,log,out,toc,lof,lot,bbl,bcf,blg,run.xml}
rm -rf report/_minted-*

# Create _minted directory
mkdir -p report/_minted-main

# First LaTeX pass with shell-escape
TEXINPUTS="./report/:" pdflatex -shell-escape -interaction=nonstopmode -output-directory=report report/main.tex

# Run Biber for bibliography
cd report
biber main
cd ..

# Second LaTeX pass
TEXINPUTS="./report/:" pdflatex -shell-escape -interaction=nonstopmode -output-directory=report report/main.tex

# Final LaTeX pass for cross-references
TEXINPUTS="./report/:" pdflatex -shell-escape -interaction=nonstopmode -output-directory=report report/main.tex

echo "Compilation complete. Check report/main.pdf for the result."
