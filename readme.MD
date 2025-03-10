Start docker container first, assumes C:\Temp\deepface exists
docker run -p 5005:5000 -v C:\Temp\deepface\dataset:/img_db -v C:\Temp\deepface\weights:/root/.deepface/weights serengil/deepface