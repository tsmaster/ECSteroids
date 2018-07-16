

all: components.cs

install: components.cs
	bash ./install.sh

components.cs: components.xml
	python genCS.py
