# BSc Computer Science Dissertation

## Development of a Configuration GUI for MotionInput
### Enhancing Gaming Accessibility Through Visual Profile Management

This repository contains the LaTeX source files for my BSc Computer Science dissertation at UCL.

## Repository Structure

```
dissertation/
├── report/
│   ├── chapters/           # Main dissertation chapters
│   │   ├── title.tex
│   │   ├── abstract.tex
│   │   ├── declaration.tex
│   │   ├── acknowledgements.tex
│   │   ├── introduction.tex
│   │   ├── background.tex
│   │   ├── requirements.tex
│   │   ├── implementation.tex
│   │   ├── testing.tex
│   │   ├── conclusion.tex
│   │   └── appendices.tex
│   ├── bibliography/       # Bibliography files
│   │   └── references.bib
│   ├── styles/            # LaTeX style files
│   │   └── ucl_thesis.sty
│   └── main.tex           # Main LaTeX document
└── compile.sh             # Compilation script
```

## Requirements

- TeX Live 2024 or newer
- Python with Pygments (for minted package)
- biber (for bibliography)

## Compilation

The dissertation can be compiled using the provided script:

```bash
./compile.sh
```

This script:
1. Cleans previous compilation files
2. Runs pdflatex with shell-escape (required for minted)
3. Processes bibliography with biber
4. Runs pdflatex twice more for references

The final PDF will be generated as `report/main.pdf`.

## Style Guide

- The document uses the UCL thesis style file (`ucl_thesis.sty`)
- Bibliography follows the IEEE citation style
- Code listings use the minted package for syntax highlighting
- Document is formatted for A4 paper with UCL-specified margins

## Tools Used

- WinUI 3 Framework
- ONNX Runtime
- Stable Diffusion
- Visual Studio 2022
- LaTeX with minted for code highlighting

## Contact

Yuma Noguchi  
UCL Computer Science  
Academic Year 2023/24
