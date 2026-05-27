package main

import (
	"fmt"
	"time"
)

func main() {
	ticker := time.NewTicker(15 * time.Second)
	defer ticker.Stop()

	for i := 0; i < 10; i++ {
		<-ticker.C
		fmt.Println("Task executed at:", time.Now())
	}
}
